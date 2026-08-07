using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct Dockable : IComponentData
    {
        public float3 PointA;
        public float3 PointB;
    }
}
