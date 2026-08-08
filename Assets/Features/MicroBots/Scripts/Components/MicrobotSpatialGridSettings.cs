using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotSpatialGridSettings : IComponentData
    {
        public float CellSize;

        // Max height Navigation is allowed to select as one intermediate climbing hop.
        public float MaxHopHeight;
    }
}
