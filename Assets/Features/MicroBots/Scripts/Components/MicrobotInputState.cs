using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotInputState : IComponentData
    {
        public bool ToggleBase;
        public float3 MoveInput;
    }
}
