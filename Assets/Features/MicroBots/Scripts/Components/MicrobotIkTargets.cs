using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotIkTargets : IComponentData
    {
        public float3 TargetAPos;
        public float3 TargetBPos;
    }
}
