using Unity.Entities;
using Unity.Mathematics;

namespace HippoLib.MicroBots
{
    public struct MicrobotSegments : IComponentData
    {
        public Entity SegmentAEntity;
        public quaternion RotationA;
        public float LengthA;
        public Entity SegmentBEntity;
        public quaternion RotationB;
        public float LengthB;
    }
}
