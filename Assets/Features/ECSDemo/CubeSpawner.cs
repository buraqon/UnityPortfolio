using Unity.Entities;

namespace HippoLib.ECSDemo
{
    public struct CubeSpawner : IComponentData
    {
        public Entity Prefab;
        public int Count;
        public float Spacing;
        public float Interval;
        public double NextSpawnTime;
    }
}
