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

        // Runtime state
        public bool Initialized;
        public float3 StepStartPosition;
        public float StepSignedDistance;
        public float StepProgress;
        public float HeadingAngle;
        public bool HasStepSizeOverride;
        public float StepSizeOverride;
        public bool HasStepHeightOverride;
        public float StepHeightOverride;
        public float StepTargetHeight;
        public bool ForceEnd;
    }
}
