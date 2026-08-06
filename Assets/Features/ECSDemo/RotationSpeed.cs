using Unity.Entities;

namespace HippoLib.ECSDemo
{
    public struct RotationSpeed : IComponentData
    {
        public float RadiansPerSecond;
    }
}
