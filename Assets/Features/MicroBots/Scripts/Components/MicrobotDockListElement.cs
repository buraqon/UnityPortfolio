using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotDockListElement : IBufferElementData
    {
        public Entity DockEntity;
    }
}
