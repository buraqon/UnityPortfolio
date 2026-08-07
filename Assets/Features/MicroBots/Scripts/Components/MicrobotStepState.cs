using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotStepState : IComponentData
    {
        public bool Initialized;
        public float3 StepStartPosition;
        public float3 StepTargetPosition;
        public float StepProgress;
        public float HeadingAngle;
    }
}
