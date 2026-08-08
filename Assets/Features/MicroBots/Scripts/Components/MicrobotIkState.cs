using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotIkState : IComponentData
    {
        public bool BaseIsSegmentB;
        public bool Initialized;
    }
}
