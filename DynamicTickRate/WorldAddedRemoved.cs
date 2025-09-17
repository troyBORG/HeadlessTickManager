using FrooxEngine;

namespace DynamicTickRate;

public partial class DynamicTickRate
{
    private static void OnWorldAddedRemoved(World world)
    {
        Controller?.OnWorldAdded(world);
        world.WorldDestroyed += OnWorldRemoved;
    }

    private static void OnWorldRemoved(World world)
    {
        Controller?.OnWorldRemoved(world);
    }
}
