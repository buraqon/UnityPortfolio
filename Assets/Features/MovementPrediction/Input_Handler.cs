using System;
using Unity.Netcode;
using UnityEngine;

public class Input_Handler : NetworkBehaviour
{
    private bool isActive = false;

    protected Vector2 moveDirection;
    protected Vector3 lookDirection;
    protected bool jumpPressed;
    protected bool sprintPressed;
    protected Transform characterTransform;

    public void SetCharacterTransform(Transform t)
    {
        characterTransform = t;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        Init();
    }

    protected virtual void Init()
    {
    }
    private void Update()
    {
        if (!IsOwner || !isActive)
            return;

        UpdateAiming();

        OnSampleFrameInput();
    }

    protected virtual void OnSampleFrameInput() { }

    public void SampleTick()
    {
        if (!IsOwner || !isActive)
            return;

        UpdateMovement();
        SendAiming();
    }

    private void SendAiming()
    {
        OnSendAiming();
    }

    protected virtual void OnSendAiming() { }


    private void UpdateAiming()
    {
        OnUpdateAimaing();
    }

    protected virtual void OnUpdateAimaing() { }

    private void UpdateMovement()
    {
        OnUpdateMovement();
    }

    protected virtual void OnUpdateMovement() { }
    
    public virtual void ResolveTickInput(int tick) { }

    public Vector2 GetMoveDirection() => moveDirection;

    public virtual Vector3 GetWorldMoveDirection()
    {
        if (characterTransform == null) return Vector3.zero;
        return characterTransform.forward * moveDirection.y + characterTransform.right * moveDirection.x;
    }

    public Vector3 GetLookDirection() => lookDirection;
    public bool JumpPressed() => jumpPressed;
    public bool SprintPressed() => sprintPressed;

    public MovementState GetMovementState()
    {
        MovementState state = MovementState.None;

        if (SprintPressed())
            state |= MovementState.Sprinting;

        if (JumpPressed())
            state |= MovementState.Jumping;
        return state;
    }

    public void SetActive(bool b)
    {
        isActive = b;
    }
}


[Flags]
public enum MovementState
{
    None = 0,
    Sprinting = 1 << 0,
    Crouching = 1 << 1,
    Jumping = 1 << 2,
}
