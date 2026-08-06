using Unity.Entities;
using UnityEngine;

namespace HippoLib.MicroBots
{
    public class MicrobotStepSettings : IComponentData
    {
        public float StepSize;
        public float StepSpeed;
        public float StepHeight;
        public AnimationCurve StepCurve;
    }
}
