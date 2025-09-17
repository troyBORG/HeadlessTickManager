using FrooxEngine;

namespace DynamicTickRate;

public partial class DynamicTickRate
{
    private static void OnUserJoinLeave(World world)
    {
        world.UserJoined += OnUserJoin;
        world.UserLeft   += OnUserLeave;
    }

    private static void OnUserJoin(User user)
    {
        if (user.IsHost) return;
        Controller?.OnUserJoin(user.World);
    }

    private static void OnUserLeave(User user)
    {
        if (user.IsHost) return;
        Controller?.OnUserLeave(user.World);
    }
}
