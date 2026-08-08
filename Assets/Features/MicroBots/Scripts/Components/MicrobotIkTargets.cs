using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotIkTargets : IComponentData
    {
        public Entity TargetAEntity;
        public Entity TargetBEntity;
    }
}
