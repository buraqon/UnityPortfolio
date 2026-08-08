using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    // Climbable points (separate from Dockable, which is extremity-only for shape-forming).
    public struct MicrobotClimbPoints : IComponentData
    {
        public float3 PointA;
        public float3 PointB;
        public float3 Elbow;
    }
}
