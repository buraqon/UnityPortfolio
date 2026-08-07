using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotIkTargets : IComponentData
    {
        public Entity TargetAEntity;
        public Entity TargetBEntity;
        public float3 TargetAOffset;
        public float3 TargetBOffset;
    }
}
