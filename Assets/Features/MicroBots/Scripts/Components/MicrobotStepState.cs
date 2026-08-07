using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotStepState : IComponentData
    {
        // Authored settings
        public float StepSize;
        public float StepSpeed;
        public float StepHeight;
        public float TurnSpeed;
        public float TurnGate;
        public float HeadingEpsilon;

        // Active goal for the currently-free extremity. Persists across multiple steps (and
        // suppresses the normal per-landing toggle) until satisfied. When absent, stepping falls
        // back to plain WASD-driven walking.
        public bool HasGoal;
        public float3 GoalPoint;
        public float GoalTolerance;

        // Runtime step-in-progress state
        public bool Initialized;
        public float3 StepStartPosition;
        public float StepSignedDistance;
        public float StepTargetHeight;
        public float StepProgress;
        public float HeadingAngle;
        public bool IsFinalApproach;
    }
}
