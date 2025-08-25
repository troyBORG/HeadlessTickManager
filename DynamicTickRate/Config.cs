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
}
