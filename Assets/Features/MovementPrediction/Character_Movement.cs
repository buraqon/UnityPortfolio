using Unity.Netcode;
using UnityEngine;

// Ties Input_Handler + Movement_Controller + PredictedTransform together for this demo: reads
// this tick's input, runs one simulation step, and hands the result to PredictedTransform for
// its replay buffer. SimulateTick is that same simulation step, re-run in isolation from buffered
// past input during reconciliation replay - the two must stay in lockstep or replay would diverge
// from what actually happened live.
public class Character_Movement : Movement_Controller
{
    [SerializeField] private float startSpeed = 5f;

    private Input_Handler inputHandler;
    private PredictedTransform predictedTransform;

    private NetworkTickSystem tickSystem => NetworkManager.Singleton.NetworkTickSystem;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

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
}
