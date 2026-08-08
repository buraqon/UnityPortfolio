using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    [UpdateAfter(typeof(MicrobotInputSystem))]
    [UpdateBefore(typeof(MicrobotStepMovementSystem))]
    public partial struct MicrobotNavigationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            var deltaTime = SystemAPI.Time.DeltaTime;

            var hasGrid = SystemAPI.TryGetSingleton<MicrobotSpatialGrid>(out var grid);
            var maxHopHeight = SystemAPI.TryGetSingleton<MicrobotSpatialGridSettings>(out var gridSettings)
                ? gridSettings.MaxHopHeight
                : 0f;

            var climbPointsLookup = SystemAPI.GetComponentLookup<MicrobotClimbPoints>(true);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            ProcessDockCommands(ref state, deltaTime, hasGrid, grid, maxHopHeight, climbPointsLookup, transformLookup);
            ProcessFollowCommand(ref state, hasGrid, grid, maxHopHeight, climbPointsLookup, transformLookup);
        }

        private void ProcessDockCommands(
            ref SystemState state,
            float deltaTime,
            bool hasGrid,
            in MicrobotSpatialGrid grid,
            float maxHopHeight,
            ComponentLookup<MicrobotClimbPoints> climbPointsLookup,
            ComponentLookup<LocalTransform> transformLookup)
        {
            var dockableLookup = SystemAPI.GetComponentLookup<Dockable>(true);
            var ikTargetsLookup = SystemAPI.GetComponentLookup<MicrobotIkTargets>(true);
            var ikStateLookup = SystemAPI.GetComponentLookup<MicrobotIkState>(true);
            var goalLookup = SystemAPI.GetComponentLookup<MicrobotGoal>(false);

            foreach (var (dockCommand, dockList) in SystemAPI
                         .Query<RefRW<MicrobotDockCommand>, DynamicBuffer<MicrobotDockListElement>>())
            {
                if (dockList.Length == 0)
                    continue;

                if (dockCommand.ValueRO.Docked)
                {
                    dockCommand.ValueRW.RestTimer -= deltaTime;
                    if (dockCommand.ValueRO.RestTimer <= 0f)
                    {
                        dockCommand.ValueRW.CurrentDockIndex =
                            (dockCommand.ValueRO.CurrentDockIndex + 1) % dockList.Length;
                        dockCommand.ValueRW.Docked = false;
                        dockCommand.ValueRW.PointAClaimed = false;
                        dockCommand.ValueRW.PointBClaimed = false;
                    }

                    continue;
                }

                var microbotEntity = dockCommand.ValueRO.MicrobotEntity;
                var dockEntity = dockList[dockCommand.ValueRO.CurrentDockIndex].DockEntity;
                if (!ikTargetsLookup.HasComponent(microbotEntity) || !dockableLookup.HasComponent(dockEntity))
                    continue;

                var ikTargets = ikTargetsLookup[microbotEntity];
                var dockPoints = dockableLookup[dockEntity];

                var posA = ikTargets.TargetAPos;
                var posB = ikTargets.TargetBPos;

                var tolerance = dockCommand.ValueRO.Tolerance;

                var pointAClaimed = dockCommand.ValueRO.PointAClaimed
                                    || math.distance(posA, dockPoints.PointA) <= tolerance
                                    || math.distance(posB, dockPoints.PointA) <= tolerance;
                var pointBClaimed = dockCommand.ValueRO.PointBClaimed
                                    || math.distance(posA, dockPoints.PointB) <= tolerance
                                    || math.distance(posB, dockPoints.PointB) <= tolerance;

                dockCommand.ValueRW.PointAClaimed = pointAClaimed;
                dockCommand.ValueRW.PointBClaimed = pointBClaimed;

                if (pointAClaimed && pointBClaimed)
                {
                    dockCommand.ValueRW.Docked = true;
                    dockCommand.ValueRW.RestTimer = dockCommand.ValueRO.RestTime;
                    continue;
                }

                var goalState = goalLookup[microbotEntity];
                if (goalState.HasGoal)
                    continue;

                var ikState = ikStateLookup[microbotEntity];
                var anchorIsB = ikState.BaseIsSegmentB;
                var anchorPos = anchorIsB ? ikTargets.TargetBPos : ikTargets.TargetAPos;
                var freePos = anchorIsB ? ikTargets.TargetAPos : ikTargets.TargetBPos;

                float3 finalGoal;
                if (!pointAClaimed && !pointBClaimed)
                {
                    finalGoal = math.distance(freePos, dockPoints.PointA) <= math.distance(freePos, dockPoints.PointB)
                        ? dockPoints.PointA
                        : dockPoints.PointB;
                }
                else
                {
                    finalGoal = pointAClaimed ? dockPoints.PointB : dockPoints.PointA;
                }

                ArmGoal(ref goalState, anchorPos, finalGoal, tolerance, hasGrid, grid, maxHopHeight,
                    climbPointsLookup, transformLookup, microbotEntity);
                goalLookup[microbotEntity] = goalState;
            }
        }

        private void ProcessFollowCommand(
            ref SystemState state,
            bool hasGrid,
            in MicrobotSpatialGrid grid,
            float maxHopHeight,
            ComponentLookup<MicrobotClimbPoints> climbPointsLookup,
            ComponentLookup<LocalTransform> transformLookup)
        {
            if (!SystemAPI.TryGetSingleton<MicrobotFollowCommand>(out var followCommand))
                return;

            foreach (var (goalComponent, ikTargets, ikState, entity) in SystemAPI
                         .Query<RefRW<MicrobotGoal>, RefRO<MicrobotIkTargets>, RefRO<MicrobotIkState>>()
                         .WithAll<MicrobotTag>()
                         .WithNone<MicrobotIgnoresFollowCommand>()
                         .WithEntityAccess())
            {
                if (goalComponent.ValueRO.HasGoal)
                    continue;

                // Only one extremity needs to land on the destination - once either has, the
                // follow command is satisfied for this bot; don't drag the other one there too.
                var arrived =
                    math.distance(ikTargets.ValueRO.TargetAPos, followCommand.Destination) <= followCommand.Tolerance ||
                    math.distance(ikTargets.ValueRO.TargetBPos, followCommand.Destination) <= followCommand.Tolerance;
                if (arrived)
                    continue;

                var anchorIsB = ikState.ValueRO.BaseIsSegmentB;
                var anchorPos = anchorIsB ? ikTargets.ValueRO.TargetBPos : ikTargets.ValueRO.TargetAPos;

                ArmGoal(ref goalComponent.ValueRW, anchorPos, followCommand.Destination, followCommand.Tolerance,
                    hasGrid, grid, maxHopHeight, climbPointsLookup, transformLookup, entity);
            }
        }

        // Shared by both goal sources: resolve the actual point to walk toward (which may be an
        // intermediate climbing hop rather than the raw destination) and arm the goal with it.
        private void ArmGoal(
            ref MicrobotGoal goal,
            float3 anchorPos,
            float3 destination,
            float tolerance,
            bool hasGrid,
            in MicrobotSpatialGrid grid,
            float maxHopHeight,
            ComponentLookup<MicrobotClimbPoints> climbPointsLookup,
            ComponentLookup<LocalTransform> transformLookup,
            Entity self)
        {
            goal.HasGoal = true;
            goal.GoalPoint = ResolveGoalPoint(anchorPos, destination, tolerance, hasGrid, grid, maxHopHeight,
                climbPointsLookup, transformLookup, self);
            goal.GoalTolerance = tolerance;
        }

        // Greedy climbing path: if the final destination's height is already within reach (including
        // plain floor-to-floor cases, where no search happens at all), go straight there. Otherwise,
        // hop to the highest currently-reachable climb point that doesn't overshoot past the final
        // destination's height - repeated every time a leg completes, so a bot keeps hopping upward
        // until it's finally close enough in height to aim at the real destination directly.
        private static float3 ResolveGoalPoint(
            float3 anchorPos,
            float3 finalDestination,
            float tolerance,
            bool hasGrid,
            in MicrobotSpatialGrid grid,
            float maxHopHeight,
            ComponentLookup<MicrobotClimbPoints> climbPointsLookup,
            ComponentLookup<LocalTransform> transformLookup,
            Entity self)
        {
            if (!hasGrid || math.abs(finalDestination.y - anchorPos.y) <= tolerance)
                return finalDestination;

            var anchorCell = new int2((int)math.floor(anchorPos.x / grid.CellSize), (int)math.floor(anchorPos.z / grid.CellSize));
            var bestHeight = anchorPos.y;
            // Fallback if nothing climbable is reachable: walk toward the destination's X/Z but stay at
            // the anchor's current (already-verified-safe) height - never hand Step an unreachable
            // height, since it now trusts GoalPoint.y unconditionally once horizontally close.
            var bestPoint = new float3(finalDestination.x, anchorPos.y, finalDestination.z);

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dz = -1; dz <= 1; dz++)
                {
                    var cell = anchorCell + new int2(dx, dz);
                    if (!grid.Cells.TryGetFirstValue(cell, out var candidate, out var iterator))
                        continue;

                    do
                    {
                        if (candidate == self || !transformLookup.HasComponent(candidate) || !climbPointsLookup.HasComponent(candidate))
                            continue;

                        var candidatePos = transformLookup[candidate].Position;
                        var horizontalDistance = math.distance(
                            new float2(anchorPos.x, anchorPos.z),
                            new float2(candidatePos.x, candidatePos.z));

                        if (horizontalDistance > grid.CellSize)
                            continue;

                        var climbPoints = climbPointsLookup[candidate];
                        ConsiderHop(climbPoints.PointA, anchorPos.y, finalDestination.y, maxHopHeight, ref bestHeight, ref bestPoint);
                        ConsiderHop(climbPoints.PointB, anchorPos.y, finalDestination.y, maxHopHeight, ref bestHeight, ref bestPoint);
                        ConsiderHop(climbPoints.Elbow, anchorPos.y, finalDestination.y, maxHopHeight, ref bestHeight, ref bestPoint);
                    } while (grid.Cells.TryGetNextValue(out candidate, ref iterator));
                }
            }

            return bestPoint;
        }

        private static void ConsiderHop(float3 point, float anchorHeight, float finalHeight, float maxHopHeight,
            ref float bestHeight, ref float3 bestPoint)
        {
            if (point.y > anchorHeight + maxHopHeight)
                return;
            if (point.y > finalHeight)
                return;
            if (point.y <= bestHeight)
                return;

            bestHeight = point.y;
            bestPoint = point;
        }
    }
}
