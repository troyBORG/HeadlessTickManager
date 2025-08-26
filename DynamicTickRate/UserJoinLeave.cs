using Elements.Core;
using FrooxEngine;
using ResoniteModLoader;

namespace DynamicTickRate;

public partial class DynamicTickRate : ResoniteMod
{
    private static void OnUserJoinLeave(World world)
    {
        world.UserJoined += OnUserJoin;
        world.UserLeft += OnUserLeave;
        world.WorldDestroyed += OnWorldClose;
    }

    private static void OnUserJoin(User user)
    {
        if (!user.IsHost && runner.TickRate < Config!.GetValue(MaxTickRate))
        {
            runner.TickRate = MathX.Min(runner.TickRate + Config!.GetValue(AddedTicksPerUser), Config!.GetValue(MaxTickRate));
        }
    }

    private static void OnUserLeave(User user)
    {
        if (!user.IsHost && runner.TickRate > Config!.GetValue(MinTickRate))
        {
            runner.TickRate = MathX.Max(runner.TickRate - Config!.GetValue(AddedTicksPerUser), Config!.GetValue(MinTickRate));
        }
    }

    private static void OnWorldClose(World world)
    {
        if (runner.TickRate > Config!.GetValue(MinTickRate))
        {
            runner.TickRate = MathX.Max(runner.TickRate - (Config!.GetValue(AddedTicksPerUser) * (world.UserCount - 1)), Config!.GetValue(MinTickRate));
        }
    }
}
