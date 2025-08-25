using FrooxEngine;
using ResoniteModLoader;

namespace DynamicTickRate;

public partial class DynamicTickRate : ResoniteMod
{
    public override string Name => "DynamicTickRate";
    public override string Author => "Raidriar796";
    public override string Version => "1.0.0";
    public override string Link => "https://github.com/Raidriar796/DynamicTickRate";
    public static ModConfiguration? Config;

    public override void OnEngineInit()
    {
        Config = GetConfiguration();
        Config?.Save(true);
    }

    private static StandaloneFrooxEngineRunner runner =
        (StandaloneFrooxEngineRunner)Type.GetType("FrooxEngine.Headless.Program, Resonite")!
        .GetField("runner", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
        .GetValue(null)!;
}
