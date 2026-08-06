using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    public partial struct MicrobotStepMovementSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(false);

            foreach (var (ikTarget, stepSettings, stepState) in SystemAPI
                         .Query<RefRO<MicrobotIkTarget>, MicrobotStepSettings, RefRW<MicrobotStepState>>()
                         .WithAll<MicrobotTag>())
            {
                if (!stepState.ValueRO.Initialized || stepState.ValueRO.StepProgress >= 1f)
                    continue;

                stepState.ValueRW.StepProgress += stepSettings.StepSpeed * deltaTime;
                var t = math.saturate(stepState.ValueRO.StepProgress);

                var newPosition = math.lerp(stepState.ValueRO.StepStartPosition, stepState.ValueRO.StepTargetPosition, t);
                newPosition.y += stepSettings.StepCurve.Evaluate(t) * stepSettings.StepHeight;

                var targetEntity = ikTarget.ValueRO.TargetEntity;
                var targetTransform = transforms[targetEntity];
                targetTransform.Position = newPosition;
                transforms[targetEntity] = targetTransform;
            }
        }
    }
}
