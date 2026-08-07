using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotAutoDockConfig : IComponentData
    {
        public Entity DockEntity;
        public float Tolerance;
        public float RestTime;
    }
}
