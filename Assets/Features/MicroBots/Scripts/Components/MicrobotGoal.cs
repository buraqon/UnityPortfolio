using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotGoal : IComponentData
    {
        public bool HasGoal;
        public float3 GoalPoint;
        public float GoalTolerance;
    }
}
