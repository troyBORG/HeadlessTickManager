using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using FrooxEngine;
using ResoniteModLoader;

namespace HeadlessTickManager;

public partial class HeadlessTickManager : ResoniteMod
{
    public override string Name => "HeadlessTickManager"; // keep your final name here
    public override string Author => "troyBORG";
    public override string Version => "2.0.1";
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

    public override void OnEngineInit()
    {
        try
        {
            // Ask RML for a config (may be null in your environment)
            Config = GetConfiguration();
            Config?.Save(true);

            if (Config != null && !Config.GetValue(Enable))
            {
                Msg("ℹ️ [HeadlessTickManager] Disabled via config.");
                return;
            }

            if (!ModLoader.IsHeadless)
            {
                Msg("ℹ️ [HeadlessTickManager] Not headless; skipping init.");
                return;
            }

            if (runner == null)
            {
                Msg("❌ [HeadlessTickManager] runner reflection failed (null). Is this the headless build?");
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
                Msg("🧩 [HeadlessTickManager] Using RML configuration.");
            }
            else
            {
                var cfgPath = InferConfigPath($"{Name}.json");
                if (TryReadJsonConfig(cfgPath, out tuning))
                    Msg($"🧩 [HeadlessTickManager] Loaded JSON configuration: {cfgPath}");
                else
                {
                    Msg($"⚠️ [HeadlessTickManager] Config is null; using built-in defaults. (Tried: {cfgPath})");
                    tuning = new TickTuning(); // controller defaults
                }
            }

            // Initialize controller
            runner.TickRate = tuning.MinTickRate;
            Controller = new TickController(runner, tuning, tuning.MinTickRate);

            // Hook events + backfill
            Engine.Current.WorldManager.WorldAdded += OnWorldAddedRemoved;
            Engine.Current.WorldManager.WorldAdded += OnUserJoinLeave;
            foreach (var w in Engine.Current.WorldManager.Worlds)
            {
                OnWorldAddedRemoved(w);
                OnUserJoinLeave(w);
            }


            Msg($"⚡ [HeadlessTickManager] Initialized v{Version} (Min={tuning.MinTickRate}, Max={tuning.MaxTickRate})");
        }
        catch (Exception ex)
        {
            Msg($"❌ [HeadlessTickManager] Init failed: {ex}");
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
            ResoniteMod.Msg($"❌ [HeadlessTickManager] JSON config parse failed: {ex.Message}");
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
}
