using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotIkState : IComponentData
    {
        public bool BaseIsSegmentB;
        public bool AnchorInitialized;
        public bool AnchorIsSegmentB;
        public float3 AnchorWorldPosition;
    }
}
