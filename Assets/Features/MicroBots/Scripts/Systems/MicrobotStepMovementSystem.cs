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

            foreach (var (ikTarget, ikState, stepSettings, stepState) in SystemAPI
                         .Query<RefRO<MicrobotIkTarget>, RefRO<MicrobotIkState>, RefRO<MicrobotStepSettings>, RefRW<MicrobotStepState>>()
                         .WithAll<MicrobotTag>())
            {
                var targetEntity = ikTarget.ValueRO.TargetEntity;
                var stepping = stepState.ValueRO.Initialized && stepState.ValueRO.StepProgress < 1f;

                if (!stepping && math.abs(inputState.MoveInput.y) > 0.0001f)
                {
                    var direction = math.sign(inputState.MoveInput.y);
                    var anchorPos = ikState.ValueRO.AnchorWorldPosition;
                    stepState.ValueRW.StepStartPosition = transforms[targetEntity].Position;
                    stepState.ValueRW.StepTargetPosition = anchorPos + new float3(0f, 0f, direction * stepSettings.ValueRO.StepSize);
                    stepState.ValueRW.StepProgress = 0f;
                    stepState.ValueRW.Initialized = true;
                    stepping = true;
                }

                if (!stepping)
                    continue;

                var progressBeforeAdvance = stepState.ValueRO.StepProgress;
                var newProgress = progressBeforeAdvance + stepSettings.ValueRO.StepSpeed * deltaTime;
                stepState.ValueRW.StepProgress = newProgress;
                var t = math.saturate(newProgress);

                var newPosition = math.lerp(stepState.ValueRO.StepStartPosition, stepState.ValueRO.StepTargetPosition, t);
                newPosition.y += math.sin(t * math.PI) * stepSettings.ValueRO.StepHeight;

                var targetTransform = transforms[targetEntity];
                targetTransform.Position = newPosition;
                transforms[targetEntity] = targetTransform;

                if (progressBeforeAdvance < 1f && newProgress >= 1f)
                {
                    stepLanded = true;
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
