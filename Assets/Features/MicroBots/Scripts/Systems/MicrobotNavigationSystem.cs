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

            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var dockPointsLookup = SystemAPI.GetComponentLookup<MicrobotDockPoints>(true);
            var ikTargetLookup = SystemAPI.GetComponentLookup<MicrobotIkTarget>(true);
            var ikStateLookup = SystemAPI.GetComponentLookup<MicrobotIkState>(true);
            var stepStateLookup = SystemAPI.GetComponentLookup<MicrobotStepState>(false);

            SystemAPI.TryGetSingleton<MicrobotInputState>(out var inputState);
            var moveInput = inputState.MoveInput;
            var hasCommand = false;
            var toggleRequested = false;

            foreach (var dockCommand in SystemAPI.Query<RefRW<MicrobotDockCommand>>())
            {
                if (dockCommand.ValueRO.Docked)
                    continue;

                var microbotEntity = dockCommand.ValueRO.MicrobotEntity;
                if (!ikTargetLookup.HasComponent(microbotEntity) || !dockPointsLookup.HasComponent(dockCommand.ValueRO.DockEntity))
                    continue;

                var dockPoints = dockPointsLookup[dockCommand.ValueRO.DockEntity];
                var ikState = ikStateLookup[microbotEntity];
                var stepState = stepStateLookup[microbotEntity];
                var targetEntity = ikTargetLookup[microbotEntity].TargetEntity;

                var anchorIsB = ikState.AnchorIsSegmentB;
                var anchorPos = ikState.AnchorWorldPosition;
                var freePos = transforms[targetEntity].Position;

                var posA = anchorIsB ? freePos : anchorPos;
                var posB = anchorIsB ? anchorPos : freePos;

                if (!dockCommand.ValueRO.AssignmentDecided)
                {
                    var costDirect = math.distance(posA, dockPoints.PointA) + math.distance(posB, dockPoints.PointB);
                    var costSwap = math.distance(posA, dockPoints.PointB) + math.distance(posB, dockPoints.PointA);
                    dockCommand.ValueRW.SwapAssignment = costSwap < costDirect;
                    dockCommand.ValueRW.AssignmentDecided = true;
                }

                var swap = dockCommand.ValueRO.SwapAssignment;
                var targetForA = swap ? dockPoints.PointB : dockPoints.PointA;
                var targetForB = swap ? dockPoints.PointA : dockPoints.PointB;

                var tolerance = dockCommand.ValueRO.Tolerance;
                var aReached = math.distance(posA, targetForA) <= tolerance;
                var bReached = math.distance(posB, targetForB) <= tolerance;

                if (aReached && bReached)
                {
                    dockCommand.ValueRW.Docked = true;
                    continue;
                }

                hasCommand = true;

                var freeReached = anchorIsB ? aReached : bReached;

                float3 steerFromPos;
                float3 steerToPoint;
                if (freeReached)
                {
                    // The extremity currently free to step has already reached its own point;
                    // steer toward the anchor's target instead and skip stepping so the anchor
                    // (not yet satisfied) can take over without wasting a gait cycle.
                    steerFromPos = anchorPos;
                    steerToPoint = anchorIsB ? targetForB : targetForA;
                    toggleRequested = true;
                }
                else
                {
                    steerFromPos = freePos;
                    steerToPoint = anchorIsB ? targetForA : targetForB;
                }

                var toGoal = steerToPoint - steerFromPos;
                toGoal.y = 0f;

                var turnInput = 0f;
                var forwardInput = 0f;

                var remainingDistance = math.length(toGoal);

                if (math.lengthsq(toGoal) > 0.0001f)
                {
                    var desiredHeading = math.atan2(toGoal.x, toGoal.z);
                    var rawDiff = desiredHeading - stepState.HeadingAngle;
                    var angleDiff = math.atan2(math.sin(rawDiff), math.cos(rawDiff));

                    turnInput = math.abs(angleDiff) > math.radians(stepState.HeadingEpsilon) ? math.sign(angleDiff) : 0f;
                    forwardInput = !freeReached && math.abs(angleDiff) < math.radians(stepState.TurnGate) ? 1f : 0f;
                }

                moveInput.y = forwardInput;
                moveInput.z = turnInput;

                var nominalStepSize = stepState.StepSize;
                stepState.HasStepSizeOverride = true;
                stepState.StepSizeOverride = math.min(nominalStepSize, remainingDistance);
                stepStateLookup[microbotEntity] = stepState;
            }

            if (hasCommand)
            {
                SystemAPI.SetSingleton(new MicrobotInputState
                {
                    ToggleBase = inputState.ToggleBase || toggleRequested,
                    MoveInput = moveInput
                });
            }
        }
    }
}
