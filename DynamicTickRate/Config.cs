using ResoniteModLoader;

namespace DynamicTickRate;

public partial class DynamicTickRate : ResoniteMod
{
    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> Enable =
            new ModConfigurationKey<bool>(
                "Enable",
                "Enable DynamicTickRate",
                () => true);

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<uint> MinTickRate =
            new ModConfigurationKey<uint>(
                "MinTickRate",
                "The lowest the tick rate is allowed to be set by this mod",
                () => 30);

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<uint> MaxTickRate =
            new ModConfigurationKey<uint>(
                "MaxTickRate",
                "The highest the tick rate is allowed to be set by this mod",
                () => 60);

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<uint> AddedTicksPerUser =
            new ModConfigurationKey<uint>(
                "AddedTicksPerUser",
                "How much the tick rate is increased per user",
                () => 2);

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<uint> AddedTicksPerWorld =
            new ModConfigurationKey<uint>(
                "AddedTicksPerWorld",
                "How much the tick rate is increased per world",
                () => 4);
}
