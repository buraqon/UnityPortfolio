using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotSpatialGridSettings : IComponentData
    {
        public float CellSize;

        // Max height a single step can gain above the anchor's current height.
        public float MaxClimbHeight;
    }
}
