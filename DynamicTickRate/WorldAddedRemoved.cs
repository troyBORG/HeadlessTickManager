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
        if (Engine.Current.WorldManager.WorldCount > 2)
        {
            TargetTickRate += Config!.GetValue(AddedTicksPerWorld);

            runner.TickRate = MathX.Clamp(TargetTickRate, Config!.GetValue(MinTickRate), Config!.GetValue(MaxTickRate));
        }
    }

    private static void OnWorldRemoved(World world)
    {
        if (Engine.Current.WorldManager.WorldCount > 2)
        {
            TargetTickRate -= Config!.GetValue(AddedTicksPerWorld);
            TargetTickRate -= (Config!.GetValue(AddedTicksPerUser) * (world.UserCount - 1));

            runner.TickRate = MathX.Clamp(TargetTickRate, Config!.GetValue(MinTickRate), Config!.GetValue(MaxTickRate));
        }
    }
}
