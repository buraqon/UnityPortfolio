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

    public void RestoreVelocity(Vector3 restoredVelocity)
    {
        velocity = restoredVelocity;
    }

    public Action OnMovementDoneAction;

    public Force_Movement CurrentForceMovement => currentForceMovement;

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

    public bool CanJump => currentForceMovement == null && IsGrounded();

    public void Jump(MoveParams jumpParams)
    {
        if (!CanJump)
            return;

        HandleJump(jumpParams);
    }

    protected virtual void HandleJump(MoveParams jumpParams)
    {
        OnJumped?.Invoke();
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

