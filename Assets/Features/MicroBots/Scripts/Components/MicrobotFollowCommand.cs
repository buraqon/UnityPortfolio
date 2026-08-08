using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotFollowCommand : IComponentData
    {
        public float3 Destination;
        public float Tolerance;
    }
}
