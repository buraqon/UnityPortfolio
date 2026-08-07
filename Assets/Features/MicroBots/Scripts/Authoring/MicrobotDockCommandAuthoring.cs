using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace HippoLib.MicroBots
{
    public class MicrobotDockCommandAuthoring : MonoBehaviour
    {
        public GameObject microbot;
        public List<GameObject> docks;
        public float tolerance = 0.15f;
        public float restTime = 1f;

        private class Baker : Baker<MicrobotDockCommandAuthoring>
        {
            public override void Bake(MicrobotDockCommandAuthoring authoring)
            {
                DependsOn(authoring.microbot);

                var entity = GetEntity(TransformUsageFlags.None);
                var microbotEntity = GetEntity(authoring.microbot, TransformUsageFlags.Dynamic);

                AddComponent(entity, new MicrobotDockCommand
                {
                    MicrobotEntity = microbotEntity,
                    CurrentDockIndex = 0,
                    Tolerance = authoring.tolerance,
                    RestTime = authoring.restTime
                });

                var buffer = AddBuffer<MicrobotDockListElement>(entity);
                foreach (var dock in authoring.docks)
                {
                    if (dock == null)
                        continue;

                    DependsOn(dock);
                    var dockEntity = GetEntity(dock, TransformUsageFlags.None);
                    buffer.Add(new MicrobotDockListElement { DockEntity = dockEntity });
                }
            }
        }
    }
}
