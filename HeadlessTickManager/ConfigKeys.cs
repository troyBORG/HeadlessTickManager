using ResoniteModLoader;

namespace HeadlessTickManager;

public partial class HeadlessTickManager
{
    // Enable/disable
    public static readonly ModConfigurationKey<bool> Enable =
        new("Enable", "Enable dynamic tick rate on headless", () => true);

    // Hard caps (adjust here if your hardware allows)
    public static readonly ModConfigurationKey<int> MinTickRate =
        new("MinTickRate", "Minimum tick rate", () => 30);

    // If you have strong CPU headroom, try 120–180. 144 is a good general ceiling.
    public static readonly ModConfigurationKey<int> MaxTickRate =
        new("MaxTickRate", "Maximum tick rate", () => 144);

    // Linear-ish parts
    public static readonly ModConfigurationKey<float> AddedTicksPerUser =
        new("AddedTicksPerUser", "Ticks added per (weighted) non-host user", () => 0.6f);

    public static readonly ModConfigurationKey<float> AddedTicksPerWorld =
        new("AddedTicksPerWorld", "Ticks added per extra ACTIVE world (beyond the first)", () => 1.5f);

    // What counts as an active world?
    public static readonly ModConfigurationKey<int> ActiveWorldUserThreshold =
        new("ActiveWorldUserThreshold", "Users (non-host) required for a world to be 'active'", () => 1);

    // Busy-world shaping
    public static readonly ModConfigurationKey<int> TopKWorlds =
        new("TopKWorlds", "How many busiest worlds to up-weight", () => 2);

    public static readonly ModConfigurationKey<float> BusyWorldWeight =
        new("BusyWorldWeight", "Extra weight for the busiest worlds", () => 1.75f);

    public static readonly ModConfigurationKey<int> PerWorldUserSoftCap =
        new("PerWorldUserSoftCap", "Users per world before diminishing returns", () => 16);

    public static readonly ModConfigurationKey<float> PerWorldDiminish =
        new("PerWorldDiminish", "Weight for users beyond the soft cap (0..1)", () => 0.35f);

    // Burst-join shaping
    public static readonly ModConfigurationKey<float> JoinRateTicksPerJpm =
        new("JoinRateTicksPerJpm", "Tick bonus per join-per-minute", () => 4.0f);

    public static readonly ModConfigurationKey<int> JoinWindowSeconds =
        new("JoinWindowSeconds", "Window for measuring recent joins (seconds)", () => 45);

    // Stability knobs
    public static readonly ModConfigurationKey<float> EmaAlpha =
        new("EmaAlpha", "EMA smoothing alpha (0..1, higher reacts faster)", () => 0.22f);

    public static readonly ModConfigurationKey<int> HysteresisTicks =
        new("HysteresisTicks", "Only change if >= this many ticks difference", () => 2);

    public static readonly ModConfigurationKey<int> MinChangeIntervalSeconds =
        new("MinChangeIntervalSeconds", "Minimum seconds between small changes", () => 5);

    public static readonly ModConfigurationKey<int> BigJumpThreshold =
        new("BigJumpThreshold", "A change >= this starts cooldown", () => 12);

    public static readonly ModConfigurationKey<int> BigJumpCooldownSeconds =
        new("BigJumpCooldownSeconds", "Cooldown seconds after a big jump", () => 12);

    // Logging
    public static readonly ModConfigurationKey<bool> LogOnChange =
        new("LogOnChange", "Log a line with ⚡ when tick changes", () => true);

    // Instant drop-to-idle
    public static readonly ModConfigurationKey<bool> InstantIdleDrop =
    new("InstantIdleDrop", "When no active worlds, set tick to Min immediately", () => false);
}
