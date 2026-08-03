using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;

public abstract class Movement_Handler : NetworkBehaviour
{
    [SerializeField] private float Speed = 5f;
    [SerializeField] private MoveParams moveParams;
    private float baseSpeed;
    private float speedMultiplier = 1;
    public MoveParams MoveParams => moveParams;


    private NetworkVariable<Vector3> CurrentVelocity = new
        (default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<float> CurrentAngle = new
        (default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<Vector3> lookDirection = new
        (default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Vector3 LookDirection => lookDirection.Value;

    private Vector3 cachedpos;
    private float initalGravityMulitplier;
    public Action OnJumped;
    public Action OnSpeedBoost;

    public override void OnNetworkSpawn()
    {
        baseSpeed = Speed;
        initalGravityMulitplier = moveParams.gravityMultiplier;

    }

    public void UpdateMovement(Vector2 moveDir, Vector3 lookDir)
    {
        if (IsOwner)
            lookDirection.Value = lookDir;

        OnUpdate(moveDir);
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        UpdateMovementStats();
    }

    public abstract bool IsGrounded();
    public abstract void OnUpdate(Vector2 moveDir);

    public void Look(Vector3 lookDir)
    {
        transform.forward = new Vector3(lookDir.x, 0, lookDir.z);
    }

    private void UpdateMovementStats()
    {
        var velocity = CalculateVelocity();
        CurrentVelocity.Value = velocity;
        if (velocity.magnitude >= 0.5f)
        {
            var movementAngle = Vector3.SignedAngle(velocity, transform.forward, transform.up);
            CurrentAngle.Value = movementAngle;
        }

        cachedpos = transform.position;
    }

    protected void SetSpeed(float speed)
    {
        Speed = speed;
        baseSpeed = speed;
    }

    private Vector3 CalculateVelocity()
    {
        return (transform.position - cachedpos) / Time.deltaTime;
    }

    public float GetCurrentAngle()
    {
        return CurrentAngle.Value;
    }

    public Vector3 GetCurrentVelocity()
    {
        return CurrentVelocity.Value;
    }

    public float GetSpeed()
    {
        return Speed * speedMultiplier;
    }

    public float GetBaseSpeed() => Speed;
    public float GetSpeedMultiplier() => speedMultiplier;

    public virtual void ResetMovement()
    {
        Speed = baseSpeed;
        speedMultiplier = 1;
        OnResetMovement();
    }

    protected virtual void OnResetMovement()
    {
    }

    public void SetSpeedMultiplier(float newValue)
    {
        speedMultiplier = newValue;
        OnSpeedBoost?.Invoke();
    }

    public void SetGravityMultiplier(float newValue)
    {
        moveParams.gravityMultiplier = newValue;
        moveParams.height /= 1.5f;
        OnSetGravityMultiplier();
    }

    protected virtual void OnSetGravityMultiplier()
    {
    }

    public void ResetGravityMultiplier()
    {
        moveParams.gravityMultiplier = initalGravityMulitplier;
        moveParams.height *= 1.5f;

    }
}
[System.Serializable]
public class MoveParams
{
    public float height = 10f;
    public float gravityMultiplier = 0.5f;
    public float distanceCovered = 10f;
    public float time = 1f;

    public void SetHeight(float value)
    {
        height = value;
    }
    public void SetTime(float airTime)
    {
        time = airTime;
    }
    public void SetDistance(float distance)
    {
        distanceCovered = distance;
    }
    public void SetMultiplier(float value)
    {
        gravityMultiplier = value;
    }

    public MoveParams InstantiateMoveParams()
    {
        var moveParams = new MoveParams();
        moveParams.height = height;
        moveParams.gravityMultiplier = gravityMultiplier;
        moveParams.distanceCovered = distanceCovered;
        moveParams.time = time;
        return moveParams;
    }
}