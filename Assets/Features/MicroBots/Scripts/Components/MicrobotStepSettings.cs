using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotStepSettings : IComponentData
    {
        public float StepSize;
        public float StepSpeed;
        public float StepHeight;
    }
}
