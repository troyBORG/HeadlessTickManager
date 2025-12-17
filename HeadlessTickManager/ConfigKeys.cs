using ResoniteModLoader;

namespace HeadlessTickManager;

public partial class HeadlessTickManager
{
    // Enable/disable
    public static readonly ModConfigurationKey<bool> Enable =
        new("Enable", "Enable dynamic tick rate on headless", () => true);

    public static readonly ModConfigurationKey<int> MinTickRate =
        new("MinTickRate", "Minimum tick rate", () => 30);

    public static readonly ModConfigurationKey<int> MaxTickRate =
        new("MaxTickRate", "Maximum tick rate", () => 90);

    // Linear-ish parts
    public static readonly ModConfigurationKey<float> AddedTicksPerUser =
        new("AddedTicksPerUser", "Ticks added per (weighted) non-host user", () => 0.65f);

    public static readonly ModConfigurationKey<float> AddedTicksPerWorld =
        new("AddedTicksPerWorld", "Ticks added per extra ACTIVE world (beyond the first)", () => 1.8f);

    public static readonly ModConfigurationKey<int> ActiveWorldUserThreshold =
        new("ActiveWorldUserThreshold", "Users (non-host) required for a world to be 'active'", () => 1);

    // Busy-world shaping
    public static readonly ModConfigurationKey<int> TopKWorlds =
        new("TopKWorlds", "How many busiest worlds to up-weight", () => 2);

    public static readonly ModConfigurationKey<float> BusyWorldWeight =
        new("BusyWorldWeight", "Extra weight for the busiest worlds", () => 1.6f);

    public static readonly ModConfigurationKey<int> PerWorldUserSoftCap =
        new("PerWorldUserSoftCap", "Users per world before diminishing returns", () => 16);

    public static readonly ModConfigurationKey<float> PerWorldDiminish =
        new("PerWorldDiminish", "Weight for users beyond the soft cap (0..1)", () => 0.40f);

    // Burst-join shaping
    public static readonly ModConfigurationKey<float> JoinRateTicksPerJpm =
        new("JoinRateTicksPerJpm", "Tick bonus per join-per-minute", () => 2.5f);
    
    public static readonly ModConfigurationKey<int> JoinRateMaxBonusTicks =
    new("JoinRateMaxBonusTicks", "Maximum extra ticks allowed from join bursts", () => 12);


    public static readonly ModConfigurationKey<int> JoinWindowSeconds =
        new("JoinWindowSeconds", "Window for measuring recent joins (seconds)", () => 25);

    // Stability knobs
    public static readonly ModConfigurationKey<float> EmaAlpha =
        new("EmaAlpha", "EMA smoothing alpha (0..1, higher reacts faster)", () => 0.25f);

    public static readonly ModConfigurationKey<int> HysteresisTicks =
        new("HysteresisTicks", "Only change if >= this many ticks difference", () => 2);

    public static readonly ModConfigurationKey<int> MinChangeIntervalSeconds =
        new("MinChangeIntervalSeconds", "Minimum seconds between small changes", () => 4);

    public static readonly ModConfigurationKey<int> BigJumpThreshold =
        new("BigJumpThreshold", "A change >= this starts cooldown", () => 10);

    public static readonly ModConfigurationKey<int> BigJumpCooldownSeconds =
        new("BigJumpCooldownSeconds", "Cooldown seconds after a big jump", () => 10);

    // Logging
    public static readonly ModConfigurationKey<bool> LogOnChange =
        new("LogOnChange", "Log a line when tick changes", () => true);

    public static readonly ModConfigurationKey<bool> EnablePeriodicSummaries =
        new("EnablePeriodicSummaries", "Enable periodic status summaries (every 2 hours)", () => true);

    public static readonly ModConfigurationKey<bool> EnableHealthWarnings =
        new("EnableHealthWarnings", "Enable health check warnings (max tick rate, high fluctuation)", () => true);

    // Instant drop-to-idle
    public static readonly ModConfigurationKey<bool> InstantIdleDrop =
    new("InstantIdleDrop", "When no active worlds, set tick to Min immediately", () => true);
}
