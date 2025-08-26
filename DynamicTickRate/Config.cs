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
    public static readonly ModConfigurationKey<Byte> MinTickRate =
            new ModConfigurationKey<Byte>(
                "MinTickRate",
                "The lowest the tick rate is allowed to be set by this mod",
                () => 30);

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<Byte> MaxTickRate =
            new ModConfigurationKey<Byte>(
                "MaxTickRate",
                "The highest the tick rate is allowed to be set by this mod",
                () => 60);

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<Byte> AddedTicksPerUser =
            new ModConfigurationKey<Byte>(
                "AddedTicksPerUser",
                "How much the Tick Rate is increased per user",
                () => 2);

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<Byte> AddedTicksPerWorld =
            new ModConfigurationKey<Byte>(
                "AddedTicksPerWorld",
                "How much the Tick Rate is increased per world",
                () => 4);
}
