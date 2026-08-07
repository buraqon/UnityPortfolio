using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotDockCommand : IComponentData
    {
        public Entity MicrobotEntity;
        public Entity DockEntity;
        public float Tolerance;
        public bool AssignmentDecided;
        public bool SwapAssignment;
        public bool Docked;
    }
}
