using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    // Runtime-only, created directly by MicrobotSpatialGridSystem - never baked.
    public struct MicrobotSpatialGrid : IComponentData
    {
        public NativeParallelMultiHashMap<int2, Entity> Cells;
        public float CellSize;
    }
}
