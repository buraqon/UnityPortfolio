using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotDockCommand : IComponentData
    {
        public Entity MicrobotEntity;
        public Entity DockEntity;
        public float Tolerance;
        public bool PointAClaimed;
        public bool PointBClaimed;
        public bool Docked;
    }
}
