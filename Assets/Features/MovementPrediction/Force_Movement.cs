using System;
using Unity.Netcode;
using UnityEngine;

public abstract class Force_Movement
{
    public MoveParams moveParams;
    protected Vector3 velocity;
    protected float timer;
    protected static float TickDeltaTime => 1f / NetworkManager.Singleton.NetworkConfig.TickRate;

    public void StartMovement(Action onMovementDone, Vector3 direction)
    {
        OnMovementDone = onMovementDone;
        timer = moveParams.time;
        OnStartMovement(direction);
    }

    protected virtual void OnStartMovement(Vector3 direction)
    {
    }


    public abstract void UpdateMovement(Vector2 moveDir, bool isGrounded);
    public abstract void EndMovement();


    public Action OnMovementDone;

    public Vector3 GetVelocity()
    {
        return velocity;
    }

    public float GetTimer()
    {
        return timer;
    }

    // Used by PredictedTransform's reconciliation replay to rewind this forced-movement's
    // countdown and velocity to what they were right after a confirmed tick, before
    // re-simulating forward. Movement_Controller.OnUpdate overwrites its own velocity from
    // this object's GetVelocity() every tick while a forced movement is active, so both need
    // restoring together or the controller's copy gets clobbered on the very next tick.
    public void RestoreTimer(float restoredTimer)
    {
        timer = restoredTimer;
    }

    public void RestoreVelocity(Vector3 restoredVelocity)
    {
        velocity = restoredVelocity;
    }

    public Force_Movement(MoveParams moveParams)
    {
        this.moveParams = moveParams;
    }
}

public class Force_Movement_Jump : Force_Movement
{
    protected override void OnStartMovement(Vector3 direction)
    {
        velocity = GetSpeed() * direction;
        velocity.y = Mathf.Sqrt(moveParams.height * -(moveParams.gravityMultiplier * -9.81f));
    }

    public override void UpdateMovement(Vector2 moveDir, bool isGrounded)
    {
        velocity.y += -9.8f * moveParams.gravityMultiplier * TickDeltaTime;
        timer -= TickDeltaTime;

        if ((isGrounded && velocity.y < 0) || timer <= 0)
        {
            OnMovementDone.Invoke();
        }
    }

    public override void EndMovement()
    {
    }

    protected float GetSpeed()
    {
        return moveParams.distanceCovered / moveParams.time;
    }

    public Force_Movement_Jump(MoveParams moveParams) : base(moveParams)
    {
    }
}

public class Force_Movement_Charge : Force_Movement
{
    protected IMovable movable;
    protected float dashSpeed;
    private Vector3 forward;

    public Force_Movement_Charge(MoveParams moveParams, IMovable movable, float dashSpeed) :
        base(moveParams)
    {
        this.moveParams = moveParams;
        this.movable = movable;
        this.dashSpeed = dashSpeed;
        forward = movable.transform.forward;
        velocity = movable.transform.forward;
    }

    public override void UpdateMovement(Vector2 moveDir, bool isGrounded)
    {

        if (moveDir.magnitude > 0.1f)
        {
            var targetForward = movable.transform.forward;
            var localMoveDir = new Vector3(moveDir.x, 0, moveDir.y);
            var angle = Vector3.SignedAngle(Vector3.forward, localMoveDir, Vector3.up);
            targetForward = Quaternion.Euler(0, angle, 0) * targetForward;
            forward = Vector3.Lerp(forward, targetForward, 0.1f);
        }


        var forwardVel = dashSpeed * forward;
        velocity.y += -9.8f * moveParams.gravityMultiplier * TickDeltaTime;
        velocity = new Vector3(forwardVel.x, velocity.y, forwardVel.z);
        timer -= TickDeltaTime;
        if (timer <= 0)
        {
            OnMovementDone.Invoke();
        }
    }

    public override void EndMovement()
    {
        velocity = movable.transform.forward;
    }
}

public class Force_Movement_Dash : Force_Movement
{
    protected IMovable movable;
    protected float dashSpeed;
    public Force_Movement_Dash(MoveParams moveParams, IMovable movable, float dashSpeed) : base(moveParams)
    {
        this.moveParams = moveParams;
        this.movable = movable;
        this.dashSpeed = dashSpeed;
    }

    protected override void OnStartMovement(Vector3 direction)
    {
        velocity = dashSpeed * direction;
    }

    public override void UpdateMovement(Vector2 moveDir, bool isGrounded)
    {
        velocity.y += -9.8f * moveParams.gravityMultiplier * TickDeltaTime;
        timer -= TickDeltaTime;
        if (timer <= 0)
        {
            OnMovementDone.Invoke();
        }
    } 

    public override void EndMovement()
    {
    }
}

public class Force_Movement_GorillaUlt : Force_Movement_Jump
{
    private Action onMaxHeightReached;
    private float timeFloating;
    private float floatTimer;
    private bool isMaxHeightReached;

    public Force_Movement_GorillaUlt(MoveParams moveParams, Action onMaxHeightReached, float timeFloating) :
        base(moveParams)
    {
        this.moveParams = moveParams;
        this.onMaxHeightReached = onMaxHeightReached;
        this.timeFloating = timeFloating;
        floatTimer = 0;
    }

    public override void UpdateMovement(Vector2 moveDir, bool isGrounded)
    {
        velocity.y += -9.8f * moveParams.gravityMultiplier * TickDeltaTime;


        if (velocity.y <= 0 && !isMaxHeightReached)
        {
            onMaxHeightReached.Invoke();
            moveParams.SetMultiplier(0);
            isMaxHeightReached = true;
        }

        if (isMaxHeightReached)
        {
            floatTimer += TickDeltaTime;
            if (floatTimer >= timeFloating)
            {
                OnMovementDone.Invoke();
            }
        }
    }

    public void Interrupt()
    {
        OnMovementDone.Invoke();
    }
}

public class Force_Movement_Grapple : Force_Movement
{
    private Vector3 targetPos;
    private IMovable movable;
    private Vector3 moveDirection;

    public Force_Movement_Grapple(MoveParams moveParams, Vector3 targetPos, IMovable movable) : base(moveParams)
    {
        this.moveParams = moveParams;
        this.targetPos = targetPos;
        this.movable = movable;
        moveDirection = (targetPos - movable.transform.position).normalized;
    }

    public override void UpdateMovement(Vector2 moveDir, bool isGrounded)
    {

        velocity = moveDirection * moveParams.distanceCovered / moveParams.time;
        velocity.y += -9.8f * moveParams.gravityMultiplier * TickDeltaTime;
        timer -= TickDeltaTime;
        if (Vector3.Distance(movable.transform.position, targetPos) < 0.5f || timer <= 0)
        {
            OnMovementDone.Invoke();

        }
    }

    public override void EndMovement()
    {
    }
}