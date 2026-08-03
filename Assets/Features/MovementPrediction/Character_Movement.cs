using System;
using Unity.Netcode;
using UnityEngine;

// Ties input + movement simulation + PredictedTransform together for this demo: reads this
// tick's input, runs one simulation step, and hands the result to PredictedTransform for its
// replay buffer. SimulateTick is that same simulation step, re-run in isolation from buffered
// past input during reconciliation replay - the two must stay in lockstep or replay would diverge
// from what actually happened live.
public class Character_Movement : NetworkBehaviour
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

    [SerializeField] protected CharacterController controller;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] float rayDistance = 0.1f;
    private const float gravity = -9.81f;
    protected static float TickDeltaTime => 1f / NetworkManager.Singleton.NetworkConfig.TickRate;
    protected Vector3 velocity = new Vector3(0, -0.2f, 0);
    private Force_Movement currentForceMovement;
    protected bool isActive = true;

    public Vector3 Velocity => velocity;
    public Action OnMovementDoneAction;
    public Force_Movement CurrentForceMovement => currentForceMovement;

    [SerializeField] private float startSpeed = 5f;

    private Input_Handler inputHandler;
    private PredictedTransform predictedTransform;

    private NetworkTickSystem tickSystem => NetworkManager.Singleton.NetworkTickSystem;

    public override void OnNetworkSpawn()
    {
        baseSpeed = Speed;
        initalGravityMulitplier = moveParams.gravityMultiplier;

        inputHandler = GetComponent<Input_Handler>();
        predictedTransform = GetComponent<PredictedTransform>();

        SetStartSpeed(startSpeed);

        if (inputHandler == null)
            return;

        inputHandler.SetCharacterTransform(transform);
        inputHandler.SetActive(true);

        if (IsOwner || IsServer)
            tickSystem.Tick += OnTick;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (NetworkManager.Singleton != null)
            tickSystem.Tick -= OnTick;
    }

    public void SetStartSpeed(float speed) => SetSpeed(speed);

    private void OnTick()
    {
        var tick = tickSystem.LocalTime.Tick;

        inputHandler.SampleTick();
        inputHandler.ResolveTickInput(tick);

        var moveDir = inputHandler.GetMoveDirection();
        var worldMoveDir = inputHandler.GetWorldMoveDirection();
        var lookDir = inputHandler.GetLookDirection();
        var movementState = inputHandler.GetMovementState();
        var jumpPressed = inputHandler.JumpPressed();

        SimulateTick(moveDir, worldMoveDir, lookDir, movementState, jumpPressed);

        predictedTransform?.RecordTickState(tick, moveDir, worldMoveDir, lookDir, movementState, jumpPressed, Velocity, CurrentForceMovement);
    }

    public void SimulateTick(Vector2 moveDir, Vector3 worldMoveDir, Vector3 lookDir, MovementState movementState, bool jumpPressed)
    {
        if (lookDir.sqrMagnitude > 0.0001f)
            Look(lookDir);

        CalculateVelocity(worldMoveDir, movementState);

        if (jumpPressed)
            Jump(MoveParams);

        OnUpdate(moveDir);
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
        currentForceMovement = null;
        velocity = Vector3.zero;
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
        velocity.y = 0;
    }

    public void ResetGravityMultiplier()
    {
        moveParams.gravityMultiplier = initalGravityMulitplier;
        moveParams.height *= 1.5f;
    }

    public void RestoreVelocity(Vector3 restoredVelocity)
    {
        velocity = restoredVelocity;
    }

    public void CalculateVelocity(Vector3 worldMoveDirection, MovementState movementState)
    {
        if (currentForceMovement != null)
            return;

        var horizontalVel = new Vector3(worldMoveDirection.x, 0, worldMoveDirection.z) * GetSpeed();
        velocity = new Vector3(horizontalVel.x, velocity.y, horizontalVel.z);
    }

    public void OnUpdate(Vector2 moveDir)
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

    public bool IsGrounded()
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

    protected void RemoveMovement()
    {
        velocity = new Vector3(velocity.x, 0, velocity.z);
        currentForceMovement = null;
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
