using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    [UpdateAfter(typeof(MicrobotSpawnSystem))]
    public partial struct MicrobotAutoDockSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MicrobotAutoDockConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<MicrobotAutoDockConfig>();
            if (config.DockEntity == Entity.Null)
                return;

            var toAssign = new NativeList<Entity>(Allocator.Temp);

            foreach (var (stepState, entity) in SystemAPI.Query<RefRO<MicrobotStepState>>()
                         .WithAll<MicrobotTag>()
                         .WithNone<MicrobotDockAssigned>()
                         .WithEntityAccess())
            {
                toAssign.Add(entity);
            }

            foreach (var microbotEntity in toAssign)
            {
                var commandEntity = state.EntityManager.CreateEntity(ComponentType.ReadWrite<MicrobotDockCommand>());
                state.EntityManager.SetComponentData(commandEntity, new MicrobotDockCommand
                {
                    MicrobotEntity = microbotEntity,
                    Tolerance = config.Tolerance,
                    RestTime = config.RestTime
                });

                var dockList = state.EntityManager.AddBuffer<MicrobotDockListElement>(commandEntity);
                dockList.Add(new MicrobotDockListElement { DockEntity = config.DockEntity });

                state.EntityManager.AddComponent<MicrobotDockAssigned>(microbotEntity);
            }

            toAssign.Dispose();
        }
    }
}
