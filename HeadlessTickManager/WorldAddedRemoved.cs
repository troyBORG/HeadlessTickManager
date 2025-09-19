using FrooxEngine;

namespace HeadlessTickManager;

public partial class HeadlessTickManager
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
