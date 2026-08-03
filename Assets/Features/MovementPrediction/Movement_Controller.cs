using System;
using Unity.Netcode;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEngine;

public class Movement_Controller : Movement_Handler
{
    [SerializeField] protected CharacterController controller;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] float rayDistance = 0.1f;
    private const float gravity = -9.81f;
    protected static float TickDeltaTime => 1f / NetworkManager.Singleton.NetworkConfig.TickRate;
    protected Vector3 velocity = new Vector3(0, -0.2f, 0);
    private Force_Movement currentForceMovement;
    protected bool isActive = true;

    public Vector3 Velocity => velocity;

    // Used by PredictedTransform's reconciliation replay to reset velocity to what it was
    // right after a confirmed tick, before re-simulating forward from there.
    public void RestoreVelocity(Vector3 restoredVelocity)
    {
        velocity = restoredVelocity;
    }

    public Action OnMovementDoneAction;

    public Force_Movement CurrentForceMovement => currentForceMovement;

    // Takes an already-world-space direction (see Input_Handler.GetWorldMoveDirection) instead
    // of combining local input with transform.forward/right itself - that combination used to
    // happen here, using whichever machine's own (possibly stale, for a networked owner) facing
    // was current when this ran, which is exactly what caused large velocity-direction
    // mismatches between client and server during fast turns.
    public void CalculateVelocity(Vector3 worldMoveDirection, MovementState movementState)
    {
        if (currentForceMovement != null)
            return;

        var horizontalVel = new Vector3(worldMoveDirection.x, 0, worldMoveDirection.z) * GetSpeed();
        velocity = new Vector3(horizontalVel.x, velocity.y, horizontalVel.z);
    }
    public override void OnUpdate(Vector2 moveDir)
    {
        if (!isActive)
            return;

        var isGrounded = RaycastFloor();
        if (currentForceMovement != null)
        {
            velocity = currentForceMovement.GetVelocity();
            currentForceMovement.UpdateMovement(moveDir, isGrounded);
        }
        else
        {
            if (!isGrounded)
            {
                velocity.y += gravity * MoveParams.gravityMultiplier * TickDeltaTime;
            }
            else if (velocity.y < 0)
            {
                velocity.y = -0.2f;
            }
        }

        controller.Move(velocity * TickDeltaTime);
    }

    private bool RaycastFloor()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
        {
            return true;
        }
        return false;
    }

    public void Jump(MoveParams jumpParams)
    {
        if (currentForceMovement != null)
            return;

        HandleJump(jumpParams);
    }

    protected virtual void HandleJump(MoveParams jumpParams)
    {
        OnJumped.Invoke();
        velocity.y = Mathf.Sqrt(jumpParams.height * -(jumpParams.gravityMultiplier * gravity));
    }

    public override bool IsGrounded()
    {
        return RaycastFloor() || controller.isGrounded;
    }

    public void AddForceMovement(Force_Movement forcedMovement, Vector3 direction, Action onMovementDone)
    {
        if (!IsOwner && !IsServer)
            Debug.LogWarning($"AddForceMovement called on {gameObject.name} without owner/server authority - this movement will not be predicted locally.", this);

        if (currentForceMovement != null)
        {
            // Interrupt-and-replace instead of silently dropping the new request: dropping it
            // discarded the new request's onMovementDone entirely, so whichever ability's
            // pause/movement-lock state (set up by the caller before calling AddForceMovement)
            // depended on that callback for cleanup never got un-done - permanently soft-locking
            // the character. Run the interrupted movement's own completion callback first so its
            // state resets properly, then start the new one.
            currentForceMovement.EndMovement();
            OnMovementDone();
        }

        OnMovementDoneAction = onMovementDone;
        currentForceMovement = forcedMovement;
        currentForceMovement.StartMovement(OnMovementDone, direction);
    }

    private void OnMovementDone()
    {
        currentForceMovement = null;
        velocity = Vector2.zero;
        OnMovementDoneAction?.Invoke();
    }
    protected override void OnSetGravityMultiplier()
    {
        base.OnSetGravityMultiplier();
        velocity.y = 0;
    }
    protected void RemoveMovement()
    {
        velocity = new Vector3(velocity.x, 0, velocity.z);
        currentForceMovement = null;
    }
    protected override void OnResetMovement()
    {
        currentForceMovement = null;
        velocity = Vector3.zero;
    }
}

