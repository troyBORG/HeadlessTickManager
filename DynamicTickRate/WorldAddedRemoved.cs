using Elements.Core;
using FrooxEngine;
using ResoniteModLoader;

namespace DynamicTickRate;

public partial class DynamicTickRate : ResoniteMod
{
    private static void OnWorldAddedRemoved(World world)
    {
        OnWorldAdded(world);
        world.WorldDestroyed += OnWorldRemoved;
    }

    private static void OnWorldAdded(World world)
    {
        if (Engine.Current.WorldManager.WorldCount > 2 && runner.TickRate < Config!.GetValue(MaxTickRate))
        {
            runner.TickRate = MathX.Min(runner.TickRate + Config!.GetValue(AddedTicksPerWorld), Config!.GetValue(MaxTickRate));
        }
    }

    private static void OnWorldRemoved(World world)
    {
        if (Engine.Current.WorldManager.WorldCount > 2 && runner.TickRate > Config!.GetValue(MinTickRate))
        {
            runner.TickRate = MathX.Max(runner.TickRate - Config!.GetValue(AddedTicksPerWorld), Config!.GetValue(MinTickRate));
        }
    }
}
