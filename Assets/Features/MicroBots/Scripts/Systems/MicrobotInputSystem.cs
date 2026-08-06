using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

namespace HippoLib.MicroBots
{
    [UpdateBefore(typeof(MicrobotIkSystem))]
    public partial struct MicrobotInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var singletonEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singletonEntity, new MicrobotInputState());
        }

        public void OnUpdate(ref SystemState state)
        {
            var keyboard = Keyboard.current;

            var moveInput = float3.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) moveInput.y += 1f;
                if (keyboard.sKey.isPressed) moveInput.y -= 1f;
                if (keyboard.dKey.isPressed) moveInput.z += 1f;
                if (keyboard.aKey.isPressed) moveInput.z -= 1f;
            }

            SystemAPI.SetSingleton(new MicrobotInputState
            {
                ToggleBase = keyboard != null && keyboard.tKey.wasPressedThisFrame,
                MoveInput = moveInput
            });
        }
    }
}
