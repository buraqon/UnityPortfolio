using Unity.Entities;

namespace HippoLib.MicroBots
{
    public struct MicrobotToggleRequest : IComponentData
    {
        public bool Toggled;
    }
}
