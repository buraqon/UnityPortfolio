using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotMovementTarget : IComponentData
    {
        public float3 Destination;
    }
}
