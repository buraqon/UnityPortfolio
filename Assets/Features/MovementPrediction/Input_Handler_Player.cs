using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public enum InputDelayMode
{
    Disabled,
    Fixed,
    Adaptive
}

public class Input_Handler_Player : Input_Handler
{
    [SerializeField] private InputActionAsset input;
    private InputAction move;
    private InputAction look;
    private InputAction jump;
    private InputAction sprint;

    [SerializeField] private float XCameraMaxRot = 70f;
    [SerializeField] private float sensitivityMultiplier = 2f;

    private float xRotation;
    private float yRotation;

    private int lastMovementTick = -1;

    [Header("Debug")]
    [SerializeField] private bool logInputPipeline = false;

    [Header("Input Delay (experimental)")]
    [SerializeField] private InputDelayMode inputDelayMode = InputDelayMode.Disabled;
    [SerializeField] private int fixedDelayTicks = 3;
    [SerializeField] private float adaptiveDelayMinTicks = 1f;
    [SerializeField] private float adaptiveDelayMaxTicks = 10f;
    [SerializeField] private float adaptiveReevaluateInterval = 1f;

    private class DelayedMoveInput
    {
        public int tick;
        public Vector2 moveDir;
        public Vector3 worldMoveDir;
        public bool jump;
        public bool sprint;
    }

    private const int DelayBufferSize = 64; // ~2s of history at 30Hz - comfortably covers any sane delay
    private readonly DelayedMoveInput[] delayBuffer = new DelayedMoveInput[DelayBufferSize];

    private readonly DelayedMoveInput[] receivedMoveBuffer = new DelayedMoveInput[DelayBufferSize];
    private DelayedMoveInput lastConfirmedMoveInput;

    private float adaptiveDelayTicksSmoothed = -1f;
    private float lastAdaptiveEvalTime = -999f;

    protected override void Init()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            input = playerInput.actions;

        input.Enable();
        move = input.FindAction("Move");
        look = input.FindAction("Look");
        jump = input.FindAction("Jump");
        sprint = input.FindAction("Sprint");
    }

    protected override void OnUpdateAimaing()
    {
        var delta = look.ReadValue<Vector2>();
        lookDirection = GetLookDirection(delta);
    }

    protected override void OnSendAiming()
    {
        if (IsOwner)
            UpdateAimingServerRPC(lookDirection, NetworkManager.Singleton.NetworkTickSystem.LocalTime.Tick);
    }

    private int lastAimingTick = -1;

    private Vector3 GetLookDirection(Vector2 delta)
    {
        float adjustedMouseX = delta.x * CalculateRotationSpeed();
        yRotation += adjustedMouseX;

        xRotation -= delta.y * CalculateRotationSpeed();
        xRotation = Mathf.Clamp(xRotation, -XCameraMaxRot, XCameraMaxRot);

        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        Vector3 forwardDirection = rotation * Vector3.forward;
        return forwardDirection;
    }

    private float CalculateRotationSpeed()
    {
        return sensitivityMultiplier * 0.03f;
    }

    [ServerRpc]
    public void UpdateAimingServerRPC(Vector3 lookDirection, int tick)
    {
        if (tick <= lastAimingTick) return;
        lastAimingTick = tick;

        this.lookDirection = lookDirection;
    }

    private Vector3 worldMoveDirection;

    public override Vector3 GetWorldMoveDirection()
    {
        return worldMoveDirection;
    }

    public override void ResolveTickInput(int tick)
    {
        if (!IsServer || IsOwner) return;

        var buffered = tick >= 0 ? receivedMoveBuffer[tick % DelayBufferSize] : null;
        var resolved = (buffered != null && buffered.tick == tick) ? buffered : lastConfirmedMoveInput;
        if (resolved == null) return; // nothing received yet at all (e.g. just spawned)

        moveDirection = resolved.moveDir;
        worldMoveDirection = resolved.worldMoveDir;
        jumpPressed = resolved.jump;
        sprintPressed = resolved.sprint;

        if (logInputPipeline && resolved != buffered && !NetworkObject.IsOwnedByServer)
        {
            var staleMsg = $"[InputStale] netId={NetworkObjectId} name={gameObject.name} wantedTick={tick} " +
                           $"usedTick={resolved.tick} behindBy={tick - resolved.tick}";
            Debug.Log(staleMsg);
            PredictedTransform.WriteLog(true, staleMsg);
        }
    }

    protected override void OnUpdateMovement()
    {
        var sampledMoveDir = move.ReadValue<Vector2>();
        var sampledJump = jump.IsPressed();
        var sampledSprint = sprint.IsPressed();

        var sampledWorldMoveDir = characterTransform != null
            ? characterTransform.forward * sampledMoveDir.y + characterTransform.right * sampledMoveDir.x
            : Vector3.zero;

        var sendTick = NetworkManager.Singleton.NetworkTickSystem.LocalTime.Tick;

        delayBuffer[sendTick % DelayBufferSize] = new DelayedMoveInput
        {
            tick = sendTick,
            moveDir = sampledMoveDir,
            worldMoveDir = sampledWorldMoveDir,
            jump = sampledJump,
            sprint = sampledSprint
        };

        if (IsOwner)
        {
            if (logInputPipeline)
            {
                var sendMsg = $"[InputSend] netId={NetworkObjectId} name={gameObject.name} tick={sendTick} " +
                              $"moveDir=({sampledMoveDir.x:F2},{sampledMoveDir.y:F2}) " +
                              $"worldMoveDir=({sampledWorldMoveDir.x:F2},{sampledWorldMoveDir.y:F2},{sampledWorldMoveDir.z:F2}) " +
                              $"jump={sampledJump} sprint={sampledSprint}";
                Debug.Log(sendMsg);
                PredictedTransform.WriteLog(false, sendMsg);
            }

            SetMovementServerRPC(sampledMoveDir, sampledWorldMoveDir, sampledJump, sampledSprint, sendTick);
        }

        var delayTicks = GetCurrentDelayTicks();
        var readTick = sendTick - delayTicks;
        var delayed = readTick >= 0 ? delayBuffer[readTick % DelayBufferSize] : null;

        if (delayed != null && delayed.tick == readTick)
        {
            moveDirection = delayed.moveDir;
            worldMoveDirection = delayed.worldMoveDir;
            jumpPressed = delayed.jump;
            sprintPressed = delayed.sprint;
        }
        else
        {
            moveDirection = sampledMoveDir;
            worldMoveDirection = sampledWorldMoveDir;
            jumpPressed = sampledJump;
            sprintPressed = sampledSprint;
        }
    }

    private int GetCurrentDelayTicks()
    {
        switch (inputDelayMode)
        {
            case InputDelayMode.Fixed:
                return Mathf.Max(0, fixedDelayTicks);
            case InputDelayMode.Adaptive:
                return Mathf.RoundToInt(UpdateAndGetAdaptiveDelayTicks());
            default:
                return 0;
        }
    }

    private float UpdateAndGetAdaptiveDelayTicks()
    {
        if (Time.time - lastAdaptiveEvalTime < adaptiveReevaluateInterval)
            return Mathf.Max(adaptiveDelayTicksSmoothed, 0f);

        lastAdaptiveEvalTime = Time.time;

        var targetTicks = Mathf.Clamp(GetCurrentLatencyTicks(), adaptiveDelayMinTicks, adaptiveDelayMaxTicks);

        adaptiveDelayTicksSmoothed = adaptiveDelayTicksSmoothed < 0f
            ? targetTicks // first evaluation - snap instead of ramping from an undefined start
            : Mathf.MoveTowards(adaptiveDelayTicksSmoothed, targetTicks, 1f);

        return adaptiveDelayTicksSmoothed;
    }

    private float GetCurrentLatencyTicks()
    {
        var tickDurationMs = 1000f / NetworkManager.Singleton.NetworkConfig.TickRate;

        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport transport)
        {
            var rttMs = transport.GetCurrentRtt(NetworkManager.Singleton.LocalClientId);
            return rttMs * 0.5f / tickDurationMs;
        }


        return NetworkManager.Singleton.NetworkTickSystem.LocalTime.Tick -
               NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick;
    }

    [ServerRpc]
    public void SetMovementServerRPC(Vector2 m, Vector3 worldMoveDir, bool j, bool s, int tick)
    {
        if (tick <= lastMovementTick) return;
        lastMovementTick = tick;

        if (logInputPipeline && !NetworkObject.IsOwnedByServer)
        {
            var recvTick = NetworkManager.Singleton.NetworkTickSystem.LocalTime.Tick;
            var recvMsg = $"[InputRecv] netId={NetworkObjectId} name={gameObject.name} sentTick={tick} recvTick={recvTick} " +
                          $"ticksInFlight={recvTick - tick} moveDir=({m.x:F2},{m.y:F2}) " +
                          $"worldMoveDir=({worldMoveDir.x:F2},{worldMoveDir.y:F2},{worldMoveDir.z:F2}) jump={j} sprint={s}";
            Debug.Log(recvMsg);
            PredictedTransform.WriteLog(true, recvMsg);
        }

        var entry = new DelayedMoveInput
        {
            tick = tick,
            moveDir = m,
            worldMoveDir = worldMoveDir,
            jump = j,
            sprint = s
        };
        receivedMoveBuffer[tick % DelayBufferSize] = entry;
        lastConfirmedMoveInput = entry;
    }
}
