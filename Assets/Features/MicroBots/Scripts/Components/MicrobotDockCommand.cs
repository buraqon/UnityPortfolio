using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotDockCommand : IComponentData
    {
        public Entity MicrobotEntity;
        public int CurrentDockIndex;
        public float Tolerance;
        public float RestTime;
        public float RestTimer;
        public bool Resting;
        public bool PointAClaimed;
        public bool PointBClaimed;
        public bool Docked;
    }
}
