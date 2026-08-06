using Unity.Entities;

namespace HippoLib.ECSDemo
{
    public struct SpawnedCube : IBufferElementData
    {
        public Entity Value;
    }
}
