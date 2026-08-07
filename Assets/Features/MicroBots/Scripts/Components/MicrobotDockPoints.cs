using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotDockPoints : IComponentData
    {
        public float3 PointA;
        public float3 PointB;
    }
}
