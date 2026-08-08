using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    [UpdateAfter(typeof(MicrobotIkSystem))]
    public partial struct MicrobotSpatialGridSystem : ISystem
    {
        private const float DefaultCellSize = 1f;

        public void OnCreate(ref SystemState state)
        {
            var gridEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(gridEntity, new MicrobotSpatialGrid
            {
                Cells = new NativeParallelMultiHashMap<int2, Entity>(256, Allocator.Persistent),
                CellSize = DefaultCellSize
            });
        }

        public void OnDestroy(ref SystemState state)
        {
            SystemAPI.GetSingleton<MicrobotSpatialGrid>().Cells.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            var cellSize = SystemAPI.TryGetSingleton<MicrobotSpatialGridSettings>(out var settings)
                ? settings.CellSize
                : DefaultCellSize;

            var grid = SystemAPI.GetSingletonRW<MicrobotSpatialGrid>();
            grid.ValueRW.Cells.Clear();
            grid.ValueRW.CellSize = cellSize;

            foreach (var (stepState, goal, transform, entity) in SystemAPI
                         .Query<RefRO<MicrobotStepState>, RefRO<MicrobotGoal>, RefRO<LocalTransform>>()
                         .WithAll<MicrobotTag>()
                         .WithEntityAccess())
            {
                var stepping = stepState.ValueRO.Initialized && stepState.ValueRO.StepProgress < 1f;
                var isIdle = !goal.ValueRO.HasGoal && !stepping;
                if (!isIdle)
                    continue;

                var position = transform.ValueRO.Position;
                var cell = new int2((int)math.floor(position.x / cellSize), (int)math.floor(position.z / cellSize));
                grid.ValueRW.Cells.Add(cell, entity);
            }
        }
    }
}
