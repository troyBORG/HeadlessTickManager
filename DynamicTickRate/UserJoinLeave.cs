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
    }

    private static void OnUserJoin(User user)
    {
        if (!user.IsHost)
        {
            TargetTickRate += Config!.GetValue(AddedTicksPerUser);

            runner.TickRate = MathX.Clamp(TargetTickRate, Config!.GetValue(MinTickRate), Config!.GetValue(MaxTickRate));
        }
    }

    private static void OnUserLeave(User user)
    {
        if (!user.IsHost)
        {
            TargetTickRate -= Config!.GetValue(AddedTicksPerUser);

            runner.TickRate = MathX.Clamp(TargetTickRate, Config!.GetValue(MinTickRate), Config!.GetValue(MaxTickRate));
        }
    }
}
