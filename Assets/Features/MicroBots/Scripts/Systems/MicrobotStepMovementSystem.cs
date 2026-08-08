using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    [UpdateAfter(typeof(MicrobotInputSystem))]
    [UpdateBefore(typeof(MicrobotIkSystem))]
    public partial struct MicrobotStepMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            SystemAPI.TryGetSingleton<MicrobotInputState>(out var inputState);
            var deltaTime = SystemAPI.Time.DeltaTime;
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(false);
            var stepLanded = false;

            foreach (var (ikTargets, ikState, stepState) in SystemAPI
                         .Query<RefRW<MicrobotIkTargets>, RefRO<MicrobotIkState>, RefRW<MicrobotStepState>>()
                         .WithAll<MicrobotTag>()) 
            {
                var baseIsB = ikState.ValueRO.BaseIsSegmentB;
                // var anchorEntity = baseIsB ? ikTargets.ValueRO.TargetBEntity : ikTargets.ValueRO.TargetAEntity;
                // var freeEntity = baseIsB ? ikTargets.ValueRO.TargetAEntity : ikTargets.ValueRO.TargetBEntity;
                
                var anchorPos = baseIsB ? ikTargets.ValueRO.TargetBPos : ikTargets.ValueRO.TargetAPos;
                var freePos = baseIsB ? ikTargets.ValueRO.TargetAPos : ikTargets.ValueRO.TargetBPos;

                var hasGoal = stepState.ValueRO.HasGoal;

                if (hasGoal && math.distance(freePos, stepState.ValueRO.GoalPoint) <= stepState.ValueRO.GoalTolerance)
                {
                    // Already at the goal without needing to move - hand off immediately, no wasted step.
                    stepState.ValueRW.HasGoal = false;
                    stepState.ValueRW.Initialized = false;
                    stepState.ValueRW.StepProgress = 0f;
                    stepLanded = true;
                    continue;
                }

                var turnInput = 0f;
                var facingOk = true;

                if (hasGoal)
                {
                    var toGoal = stepState.ValueRO.GoalPoint - freePos;
                    toGoal.y = 0f;

                    if (math.lengthsq(toGoal) > 0.0001f)
                    {
                        var desiredHeading = math.atan2(toGoal.x, toGoal.z);
                        var rawDiff = desiredHeading - stepState.ValueRO.HeadingAngle;
                        var angleDiff = math.atan2(math.sin(rawDiff), math.cos(rawDiff));

                        turnInput = math.abs(angleDiff) > math.radians(stepState.ValueRO.HeadingEpsilon) ? math.sign(angleDiff) : 0f;
                        facingOk = math.abs(angleDiff) < math.radians(stepState.ValueRO.TurnGate);
                    }
                }
                else
                {
                    turnInput = inputState.MoveInput.z;
                }

                stepState.ValueRW.HeadingAngle += math.radians(stepState.ValueRO.TurnSpeed) * turnInput * deltaTime;

                var stepping = stepState.ValueRO.Initialized && stepState.ValueRO.StepProgress < 1f;

                if (!stepping)
                {
                    var wantsStep = hasGoal ? facingOk : math.abs(inputState.MoveInput.y) > 0.0001f;
                    if (wantsStep)
                    {
                        float stepDistance;
                        float targetHeight;
                        float direction;

                        if (hasGoal)
                        {
                            var anchorToGoal = stepState.ValueRO.GoalPoint - anchorPos;
                            anchorToGoal.y = 0f;
                            var remaining = math.length(anchorToGoal);
                            var isFinalApproach = remaining <= stepState.ValueRO.StepSize;
                            stepDistance = math.min(stepState.ValueRO.StepSize, remaining);
                            targetHeight = isFinalApproach ? stepState.ValueRO.GoalPoint.y : 0f;
                            direction = 1f;
                            stepState.ValueRW.IsFinalApproach = isFinalApproach;
                        }
                        else
                        {
                            stepDistance = stepState.ValueRO.StepSize;
                            targetHeight = 0f;
                            direction = math.sign(inputState.MoveInput.y);
                        }

                        stepState.ValueRW.StepStartPosition = freePos;
                        stepState.ValueRW.StepSignedDistance = direction * stepDistance;
                        stepState.ValueRW.StepTargetHeight = targetHeight;
                        stepState.ValueRW.StepProgress = 0f;
                        stepState.ValueRW.Initialized = true;
                        stepping = true;
                    }
                }

                if (!stepping)
                    continue;

                var progressBeforeAdvance = stepState.ValueRO.StepProgress;
                var newProgress = progressBeforeAdvance + stepState.ValueRO.StepSpeed * deltaTime;
                stepState.ValueRW.StepProgress = newProgress;
                var t = math.saturate(newProgress);

                var headingRotation = quaternion.RotateY(stepState.ValueRO.HeadingAngle);
                var forwardDir = math.rotate(headingRotation, new float3(0f, 0f, 1f));
                var liveAnchorPos = anchorPos;
                var currentStepTarget = liveAnchorPos + forwardDir * stepState.ValueRO.StepSignedDistance;
                currentStepTarget.y = stepState.ValueRO.StepTargetHeight;

                var newPosition = math.lerp(stepState.ValueRO.StepStartPosition, currentStepTarget, t);
                newPosition.y += math.sin(t * math.PI) * stepState.ValueRO.StepHeight;

                if (baseIsB)
                    ikTargets.ValueRW.TargetAPos = newPosition;
                else
                    ikTargets.ValueRW.TargetBPos = newPosition;
                    

                if (progressBeforeAdvance < 1f && newProgress >= 1f)
                {
                    if (hasGoal)
                    {
                        if (math.distance(newPosition, stepState.ValueRO.GoalPoint) <= stepState.ValueRO.GoalTolerance)
                        {
                            stepState.ValueRW.HasGoal = false;
                            stepLanded = true;
                        }
                        else if (!stepState.ValueRO.IsFinalApproach)
                        {
                            // Still far from the goal - this was an ordinary full-stride step, so toggle
                            // normally and let the gait alternate (an implicit path of steps toward the
                            // goal), instead of suppressing the toggle and getting stuck at max reach.
                            stepLanded = true;
                        }
                        // else: within final-approach range but missed tolerance - don't toggle, the same
                        // extremity takes another (re-aimed, re-sized) step next frame.
                    }
                    else
                    {
                        stepLanded = true;
                    }
                }
            }

            SystemAPI.SetSingleton(new MicrobotInputState
            {
                ToggleBase = inputState.ToggleBase || stepLanded,
                MoveInput = inputState.MoveInput
            });
        }
    }
}
