using Unity.Entities;

namespace HippoLib.MicroBots
{
    // Opt-out marker - bots with this are skipped by MicrobotNavigationSystem's follow-command pass,
    // even while a MicrobotFollowCommand singleton is present in the scene.
    public struct MicrobotIgnoresFollowCommand : IComponentData
    {
    }
}
