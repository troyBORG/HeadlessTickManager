using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FrooxEngine;
using ResoniteModLoader;

namespace HeadlessTickManager;

public partial class HeadlessTickManager : ResoniteMod
{
    public override string Name => "HeadlessTickManager"; // keep your final name here
    public override string Author => "troyBORG";
    public override string Version => "2.0.3";
    public override string Link => "https://github.com/troyBORG/HeadlessTickManager";

    public static ModConfiguration? Config;

    private static StandaloneFrooxEngineRunner? TryGetRunner()
    {
        try
        {
            var t = Type.GetType("FrooxEngine.Headless.Program, Resonite", throwOnError: false);
            if (t == null) return null;
            var f = t.GetField("runner", BindingFlags.Static | BindingFlags.NonPublic);
            if (f == null) return null;
            return f.GetValue(null) as StandaloneFrooxEngineRunner;
        }
        catch { return null; }
    }

    private static StandaloneFrooxEngineRunner? runner = TryGetRunner();
    private static TickController? Controller;
    private static StatisticsTracker? Statistics;
    private static System.Threading.Timer? SummaryTimer;
    private static System.Threading.Timer? HealthCheckTimer;

    public override void OnEngineInit()
    {
        try
        {
            // Ask RML for a config (may be null in your environment)
            Config = GetConfiguration();
            Config?.Save(true);

            if (Config != null && !Config.GetValue(Enable))
            {
                 Msg("Disabled via config.");
                return;
            }

            if (!ModLoader.IsHeadless)
            {
                 Msg("Not headless; skipping init.");
                return;
            }

            if (runner == null)
            {
               Error("runner reflection failed (null). Is this the headless build?");
                return;
            }

            // Patch SignalR BroadcastSession logging (KeyListenerAdded, Sending info, SessionInfo spam)
            // These logs are very noisy in headless servers and provide no useful info.
            var harmony = new Harmony("troyBORG.HeadlessTickManager.SignalRNoisePatch");
            SignalRNoisePatch.Apply(harmony);


            // Build tuning from either RML config or JSON fallback (System.Text.Json)
            TickTuning tuning;
            if (Config != null)
            {
                tuning = ReadFromRml(Config);
                Msg("Using RML configuration.");
            }
            else
            {
                var cfgPath = InferConfigPath($"{Name}.json");
                if (TryReadJsonConfig(cfgPath, out tuning))
                    Msg($"Loaded JSON configuration: {cfgPath}");
                else
                {
                     Warn($"Config is null; using built-in defaults. (Tried: {cfgPath})");
                    tuning = new TickTuning(); // controller defaults
                }
            }

            // Validate configuration
            var validationErrors = ValidateConfiguration(tuning);
            if (validationErrors.Count > 0)
            {
                Warn("Configuration validation found issues:");
                foreach (var error in validationErrors)
                    Warn($"  - {error}");
                Warn("Mod will continue with current values, but behavior may be unexpected.");
            }

            // Initialize statistics tracker
            Statistics = new StatisticsTracker();

            // Initialize controller
            runner.TickRate = tuning.MinTickRate;
            Controller = new TickController(runner, tuning, tuning.MinTickRate, Statistics);

            // Hook events + backfill
            Engine.Current.WorldManager.WorldAdded += OnWorldAddedRemoved;
            Engine.Current.WorldManager.WorldAdded += OnUserJoinLeave;
            int initialWorldCount = 0;
            int initialUserCount = 0;
            foreach (var w in Engine.Current.WorldManager.Worlds)
            {
                OnWorldAddedRemoved(w);
                OnUserJoinLeave(w);
                initialWorldCount++;
                initialUserCount += Math.Max(0, w.UserCount - 1); // non-host users
            }

            // Log enhanced startup summary
            LogStartupSummary(tuning, initialWorldCount, initialUserCount);

            // Start periodic summaries (every 5 minutes)
            SummaryTimer = new System.Threading.Timer(OnPeriodicSummary, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            // Start health checks (every 2 minutes)
            HealthCheckTimer = new System.Threading.Timer(OnHealthCheck, null,
                TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));

            Msg($"Initialized v{Version} (Min={tuning.MinTickRate}, Max={tuning.MaxTickRate})");
        }
            catch (Exception ex)
            {
                Error($"Init failed: {ex}");
            }
    }

    private static TickTuning ReadFromRml(ModConfiguration cfg) => new TickTuning
    {
        // caps
        MinTickRate = cfg.GetValue(MinTickRate),
        MaxTickRate = cfg.GetValue(MaxTickRate),

        // linear-ish
        AddedTicksPerUser = cfg.GetValue(AddedTicksPerUser),
        AddedTicksPerExtraWorld = cfg.GetValue(AddedTicksPerWorld),

        // activity
        ActiveWorldUserThreshold = cfg.GetValue(ActiveWorldUserThreshold),

        // shaping
        TopKWorlds = cfg.GetValue(TopKWorlds),
        BusyWorldWeight = cfg.GetValue(BusyWorldWeight),
        PerWorldUserSoftCap = cfg.GetValue(PerWorldUserSoftCap),
        PerWorldDiminish = cfg.GetValue(PerWorldDiminish),

        // join burst
        JoinRateTicksPerJpm = cfg.GetValue(JoinRateTicksPerJpm),
        JoinWindowSeconds = cfg.GetValue(JoinWindowSeconds),
        JoinRateMaxBonusTicks = cfg.GetValue(JoinRateMaxBonusTicks),


        // stability
        EmaAlpha = cfg.GetValue(EmaAlpha),
        HysteresisTicks = cfg.GetValue(HysteresisTicks),
        MinChangeIntervalSeconds = cfg.GetValue(MinChangeIntervalSeconds),
        BigJumpThreshold = cfg.GetValue(BigJumpThreshold),
        BigJumpCooldownSeconds = cfg.GetValue(BigJumpCooldownSeconds),

        // logging + idle snap
        LogOnChange = cfg.GetValue(LogOnChange),
        InstantIdleDrop = cfg.GetValue(InstantIdleDrop)
    };

    // -------- JSON fallback (System.Text.Json) --------

    private static string InferConfigPath(string fileName)
    {
        // Our DLL is in Headless/rml_mods; parent is Headless/
        var asm = Assembly.GetExecutingAssembly().Location;
        var dllDir = Path.GetDirectoryName(asm) ?? ".";
        var headlessDir = Directory.GetParent(dllDir)?.FullName ?? dllDir;
        return Path.Combine(headlessDir, "rml_config", fileName);
    }

    private static bool TryReadJsonConfig(string path, out TickTuning tuning)
    {
        tuning = new TickTuning(); // start with defaults

        try
        {
            if (!File.Exists(path)) return false;

            using var fs = File.OpenRead(path);
            using var doc = JsonDocument.Parse(fs);
            if (!doc.RootElement.TryGetProperty("values", out var values)) return false;

            // Read each value if present
            Read(values, "MinTickRate", ref tuning.MinTickRate);
            Read(values, "MaxTickRate", ref tuning.MaxTickRate);

            Read(values, "AddedTicksPerUser", ref tuning.AddedTicksPerUser);
            // note: config key is AddedTicksPerWorld, maps to AddedTicksPerExtraWorld
            Read(values, "AddedTicksPerWorld", ref tuning.AddedTicksPerExtraWorld);

            Read(values, "ActiveWorldUserThreshold", ref tuning.ActiveWorldUserThreshold);

            Read(values, "TopKWorlds", ref tuning.TopKWorlds);
            Read(values, "BusyWorldWeight", ref tuning.BusyWorldWeight);
            Read(values, "PerWorldUserSoftCap", ref tuning.PerWorldUserSoftCap);
            Read(values, "PerWorldDiminish", ref tuning.PerWorldDiminish);

            Read(values, "JoinRateTicksPerJpm", ref tuning.JoinRateTicksPerJpm);
            Read(values, "JoinWindowSeconds", ref tuning.JoinWindowSeconds);
            Read(values, "JoinRateMaxBonusTicks", ref tuning.JoinRateMaxBonusTicks);


            Read(values, "EmaAlpha", ref tuning.EmaAlpha);
            Read(values, "HysteresisTicks", ref tuning.HysteresisTicks);
            Read(values, "MinChangeIntervalSeconds", ref tuning.MinChangeIntervalSeconds);
            Read(values, "BigJumpThreshold", ref tuning.BigJumpThreshold);
            Read(values, "BigJumpCooldownSeconds", ref tuning.BigJumpCooldownSeconds);

            Read(values, "LogOnChange", ref tuning.LogOnChange);
            Read(values, "InstantIdleDrop", ref tuning.InstantIdleDrop);

            return true;
        }
        catch (Exception ex)
        {
            Error($"JSON config parse failed: {ex.Message}");
            return false;
        }
    }

    private static void Read(JsonElement obj, string name, ref int dst)
    {
        if (obj.TryGetProperty(name, out var v) && v.TryGetInt32(out var x)) dst = x;
    }
    private static void Read(JsonElement obj, string name, ref float dst)
    {
        if (obj.TryGetProperty(name, out var v))
        {
            if (v.ValueKind == JsonValueKind.Number)
            {
                if (v.TryGetDouble(out var d)) dst = (float)d;
            }
        }
    }
    private static void Read(JsonElement obj, string name, ref bool dst)
    {
        if (obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True) dst = true;
        else if (obj.TryGetProperty(name, out v) && v.ValueKind == JsonValueKind.False) dst = false;
    }

    // -------- Configuration Validation --------

    private static List<string> ValidateConfiguration(TickTuning tuning)
    {
        var errors = new List<string>();

        // Min/Max validation
        if (tuning.MinTickRate >= tuning.MaxTickRate)
            errors.Add($"MinTickRate ({tuning.MinTickRate}) must be less than MaxTickRate ({tuning.MaxTickRate})");

        if (tuning.MinTickRate < 1)
            errors.Add($"MinTickRate ({tuning.MinTickRate}) must be at least 1");

        if (tuning.MaxTickRate > 200)
            errors.Add($"MaxTickRate ({tuning.MaxTickRate}) is very high (>200), ensure your hardware can handle this");

        // EMA validation
        if (tuning.EmaAlpha < 0 || tuning.EmaAlpha > 1)
            errors.Add($"EmaAlpha ({tuning.EmaAlpha}) should be between 0 and 1");

        // Positive value checks
        if (tuning.AddedTicksPerUser < 0)
            errors.Add($"AddedTicksPerUser ({tuning.AddedTicksPerUser}) should be non-negative");

        if (tuning.AddedTicksPerExtraWorld < 0)
            errors.Add($"AddedTicksPerWorld ({tuning.AddedTicksPerExtraWorld}) should be non-negative");

        if (tuning.JoinRateTicksPerJpm < 0)
            errors.Add($"JoinRateTicksPerJpm ({tuning.JoinRateTicksPerJpm}) should be non-negative");

        if (tuning.JoinWindowSeconds < 1)
            errors.Add($"JoinWindowSeconds ({tuning.JoinWindowSeconds}) should be at least 1");

        if (tuning.ActiveWorldUserThreshold < 0)
            errors.Add($"ActiveWorldUserThreshold ({tuning.ActiveWorldUserThreshold}) should be non-negative");

        if (tuning.TopKWorlds < 1)
            errors.Add($"TopKWorlds ({tuning.TopKWorlds}) should be at least 1");

        if (tuning.BusyWorldWeight < 0)
            errors.Add($"BusyWorldWeight ({tuning.BusyWorldWeight}) should be non-negative");

        if (tuning.PerWorldUserSoftCap < 1)
            errors.Add($"PerWorldUserSoftCap ({tuning.PerWorldUserSoftCap}) should be at least 1");

        if (tuning.PerWorldDiminish < 0 || tuning.PerWorldDiminish > 1)
            errors.Add($"PerWorldDiminish ({tuning.PerWorldDiminish}) should be between 0 and 1");

        // Stability checks
        if (tuning.HysteresisTicks < 0)
            errors.Add($"HysteresisTicks ({tuning.HysteresisTicks}) should be non-negative");

        if (tuning.MinChangeIntervalSeconds < 0)
            errors.Add($"MinChangeIntervalSeconds ({tuning.MinChangeIntervalSeconds}) should be non-negative");

        if (tuning.BigJumpThreshold < 0)
            errors.Add($"BigJumpThreshold ({tuning.BigJumpThreshold}) should be non-negative");

        if (tuning.BigJumpCooldownSeconds < 0)
            errors.Add($"BigJumpCooldownSeconds ({tuning.BigJumpCooldownSeconds}) should be non-negative");

        return errors;
    }

    // -------- Enhanced Startup Summary --------

    private static void LogStartupSummary(TickTuning tuning, int worldCount, int userCount)
    {
        var instance = new HeadlessTickManager();
        Msg("=== HeadlessTickManager Startup Summary ===");
        Msg($"Version: {instance.Version}");
        Msg($"Tick Rate Range: {tuning.MinTickRate} - {tuning.MaxTickRate} Hz");
        Msg($"Initial State: {worldCount} world(s), {userCount} non-host user(s)");
        Msg("");
        Msg("Configuration:");
        Msg($"  User Scaling: {tuning.AddedTicksPerUser:F2} ticks/user, {tuning.AddedTicksPerExtraWorld:F2} ticks/extra world");
        Msg($"  Active World Threshold: {tuning.ActiveWorldUserThreshold} user(s)");
        Msg($"  Busy World Weighting: Top {tuning.TopKWorlds} worlds × {tuning.BusyWorldWeight:F2}");
        Msg($"  User Soft Cap: {tuning.PerWorldUserSoftCap} (diminish: {tuning.PerWorldDiminish:F2})");
        Msg($"  Join Burst: {tuning.JoinRateTicksPerJpm:F2} ticks/JPM, max +{tuning.JoinRateMaxBonusTicks} ticks, {tuning.JoinWindowSeconds}s window");
        Msg($"  Stability: EMA α={tuning.EmaAlpha:F2}, Hysteresis={tuning.HysteresisTicks}, MinInterval={tuning.MinChangeIntervalSeconds}s");
        Msg($"  Big Jump: Threshold={tuning.BigJumpThreshold}, Cooldown={tuning.BigJumpCooldownSeconds}s");
        Msg($"  Instant Idle Drop: {tuning.InstantIdleDrop}");
        Msg($"  Logging: {tuning.LogOnChange}");
        Msg("===========================================");
    }

    // -------- Periodic Summary --------

    private static void OnPeriodicSummary(object? state)
    {
        try
        {
            if (Controller == null || Statistics == null) return;

            var snapshot = Statistics.GetSnapshot();
            var activeWorlds = Engine.Current?.WorldManager?.Worlds?.Count(w => 
                Math.Max(0, w.UserCount - 1) >= (Controller.GetTuning()?.ActiveWorldUserThreshold ?? 1)) ?? 0;
            var totalUsers = Engine.Current?.WorldManager?.Worlds?.Sum(w => Math.Max(0, w.UserCount - 1)) ?? 0;

            Msg("=== Periodic Status Summary ===");
            Msg($"Current Tick Rate: {snapshot.CurrentTick} Hz");
            Msg($"Average (last {snapshot.HistorySize} samples): {snapshot.AverageTick:F1} Hz");
            Msg($"Peak: {snapshot.PeakTick} Hz, Min: {snapshot.MinTick} Hz");
            Msg($"Active Worlds: {activeWorlds}, Total Users: {totalUsers}");
            Msg($"Tick Changes: {snapshot.TickChangesPerHour:F1}/hour");
            if (snapshot.TimeAtMax > 60)
                Msg($"⚠ Time at Max Tick: {snapshot.TimeAtMax / 60:F1} minutes");
            Msg("================================");

            Statistics.ResetSummaryTime();
        }
        catch (Exception ex)
        {
            Error($"Periodic summary failed: {ex.Message}");
        }
    }

    // -------- Health Checks --------

    private static DateTime lastMaxTickWarning = DateTime.MinValue;
    private static int lastHealthCheckTick = 0;
    private static int healthCheckStableCount = 0;

    private static void OnHealthCheck(object? state)
    {
        try
        {
            if (Controller == null || Statistics == null || runner == null) return;

            var currentTick = (int)runner.TickRate;
            var tuning = Controller.GetTuning();
            if (tuning == null) return;

            Statistics.RecordTick(currentTick);
            Statistics.UpdateMaxMinTracking(currentTick, tuning.MinTickRate, tuning.MaxTickRate);

            var snapshot = Statistics.GetSnapshot();

            // Check if at max tick rate for extended period
            if (currentTick >= tuning.MaxTickRate)
            {
                if ((DateTime.UtcNow - lastMaxTickWarning).TotalMinutes >= 5)
                {
                    Warn($"⚠ Tick rate has been at maximum ({tuning.MaxTickRate} Hz) for {snapshot.TimeAtMax / 60:F1} minutes. Consider raising MaxTickRate if you have CPU headroom.");
                    lastMaxTickWarning = DateTime.UtcNow;
                }
            }

            // Check for wild fluctuations (high variance)
            if (snapshot.HistorySize >= 60) // Need at least 1 minute of data
            {
                // Check change rate for high fluctuation
                if (snapshot.TickChangesPerHour > 120) // More than 2 changes per minute average
                {
                    if ((DateTime.UtcNow - lastMaxTickWarning).TotalMinutes >= 10)
                    {
                        Warn($"⚠ High tick rate fluctuation detected ({snapshot.TickChangesPerHour:F1} changes/hour). Consider adjusting EmaAlpha, HysteresisTicks, or MinChangeIntervalSeconds for stability.");
                        lastMaxTickWarning = DateTime.UtcNow;
                    }
                }
            }

            // Check if tick rate is stuck (no changes)
            if (currentTick == lastHealthCheckTick)
            {
                healthCheckStableCount++;
                if (healthCheckStableCount >= 6) // 12 minutes of no changes
                {
                    // This is actually normal if idle, so only warn if we're not at min
                    if (currentTick > tuning.MinTickRate)
                    {
                        Warn($"⚠ Tick rate has been stable at {currentTick} Hz for 12+ minutes. This may indicate the system is not responding to activity changes.");
                        healthCheckStableCount = 0; // Reset to avoid spam
                    }
                }
            }
            else
            {
                healthCheckStableCount = 0;
                lastHealthCheckTick = currentTick;
            }
        }
        catch (Exception ex)
        {
            Error($"Health check failed: {ex.Message}");
        }
    }
}
