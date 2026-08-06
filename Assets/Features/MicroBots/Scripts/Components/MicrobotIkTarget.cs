using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotIkTarget : IComponentData
    {
        public Entity TargetEntity;
    }
}
