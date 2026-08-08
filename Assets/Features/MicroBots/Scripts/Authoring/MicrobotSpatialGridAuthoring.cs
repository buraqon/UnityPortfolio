using Unity.Entities;
using UnityEngine;

namespace HippoLib.MicroBots
{
    public class MicrobotSpatialGridAuthoring : MonoBehaviour
    {
        public float cellSize = 1f;
        public float maxClimbHeight = 0.3f;

        private class Baker : Baker<MicrobotSpatialGridAuthoring>
        {
            public override void Bake(MicrobotSpatialGridAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new MicrobotSpatialGridSettings
                {
                    CellSize = authoring.cellSize,
                    MaxClimbHeight = authoring.maxClimbHeight
                });
            }
        }
    }
}
