using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotSpawner : IComponentData
    {
        public Entity Prefab;
        public int SpawnCount;
        public float3 SpawnCenter;
        public float3 SpawnAreaSize;
        public float SpawnHeight;
        public bool HasSpawned;
    }
}
