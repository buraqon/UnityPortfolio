using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [UpdateAfter(typeof(MicrobotInputSystem))]
    [UpdateBefore(typeof(MicrobotIkSystem))]
    public partial struct MicrobotStepMovementSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            SystemAPI.TryGetSingleton<MicrobotInputState>(out var inputState);
            var deltaTime = SystemAPI.Time.DeltaTime;
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(false);
            var stepLanded = false;

            foreach (var (ikTarget, ikState, stepSettings, stepState) in SystemAPI
                         .Query<RefRO<MicrobotIkTarget>, RefRO<MicrobotIkState>, MicrobotStepSettings, RefRW<MicrobotStepState>>()
                         .WithAll<MicrobotTag>())
            {
                if (ikState.ValueRO.IsManualMovement)
                    continue;

                var targetEntity = ikTarget.ValueRO.TargetEntity;
                var stepping = stepState.ValueRO.Initialized && stepState.ValueRO.StepProgress < 1f;

                if (!stepping && math.abs(inputState.MoveInput.y) > 0.0001f)
                {
                    var direction = math.sign(inputState.MoveInput.y);
                    var currentPosition = transforms[targetEntity].Position;
                    stepState.ValueRW.StepStartPosition = currentPosition;
                    stepState.ValueRW.StepTargetPosition = currentPosition + new float3(0f, 0f, direction * stepSettings.StepSize);
                    stepState.ValueRW.StepProgress = 0f;
                    stepState.ValueRW.Initialized = true;
                    stepping = true;
                }

                if (!stepping)
                    continue;

                var progressBeforeAdvance = stepState.ValueRO.StepProgress;
                var newProgress = progressBeforeAdvance + stepSettings.StepSpeed * deltaTime;
                stepState.ValueRW.StepProgress = newProgress;
                var t = math.saturate(newProgress);

                var newPosition = math.lerp(stepState.ValueRO.StepStartPosition, stepState.ValueRO.StepTargetPosition, t);
                newPosition.y += stepSettings.StepCurve.Evaluate(t) * stepSettings.StepHeight;

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
