using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.InputSystem;

namespace HippoLib.MicroBots
{
    [UpdateBefore(typeof(MicrobotIkSystem))]
    public partial struct MicrobotInputSystem : ISystem
    {
        private const float TargetMoveSpeed = 1f;

        public void OnCreate(ref SystemState state)
        {
            var singletonEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singletonEntity, new MicrobotToggleRequest());
        }

        public void OnUpdate(ref SystemState state)
        {
            var keyboard = Keyboard.current;

            var toggled = keyboard != null && keyboard.tKey.wasPressedThisFrame;
            SystemAPI.SetSingleton(new MicrobotToggleRequest { Toggled = toggled });

            var moveInput = float3.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) moveInput.y += 1f;
                if (keyboard.sKey.isPressed) moveInput.y -= 1f;
                if (keyboard.dKey.isPressed) moveInput.z += 1f;
                if (keyboard.aKey.isPressed) moveInput.z -= 1f;
            }

            var moveDelta = math.normalizesafe(moveInput, float3.zero) * TargetMoveSpeed * SystemAPI.Time.DeltaTime;

            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(false);
            foreach (var ikTarget in SystemAPI.Query<RefRO<MicrobotIkTarget>>().WithAll<MicrobotTag>())
            {
                var targetEntity = ikTarget.ValueRO.TargetEntity;
                var targetTransform = transforms[targetEntity];
                targetTransform.Position += moveDelta;
                transforms[targetEntity] = targetTransform;
            }
        }
    }
}
