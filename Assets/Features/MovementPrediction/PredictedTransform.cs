using System;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class PredictedTransform : NetworkBehaviour
{
    [Header("Position Networking")] [SerializeField]
    private float positionError = 1f;

    [SerializeField] private float positionErrorPassiveFix = 0.05f;
    [SerializeField] private float positionLerpSpeed = 10f;
    [SerializeField] private int errorTickThreshold = 10;

    [SerializeField] private float hardResetDistance = 8f;

    [Header("Rotation Networking")]
    [SerializeField] private float rotationLerpSpeed = 10f;

    [Header("Debug")]
    [SerializeField] private bool logPositionError = false;
    [SerializeField] private bool logRotation = false;

    [Header("Tick Networking")] [SerializeField]
    private SyncedTransformData[] clientMovementDatas = new SyncedTransformData[BUFFERSIZE];

    [SerializeField] private NetworkVariable<SyncedTransformData> serverTransformData = new();
    [SerializeField] int currentTick;

    private ReplayTickState[] replayStates = new ReplayTickState[BUFFERSIZE];
    private Character_Movement movementController;

    private NetworkTickSystem tickSystem => NetworkManager.Singleton.NetworkTickSystem;
    private const int BUFFERSIZE = 1024;
    private int posErrorCount;

    public Vector3 ServerPosition => serverTransformData.Value.position;
    public Quaternion ServerRotation => serverTransformData.Value.rotation;

    private static bool clientLogCleared;
    private static bool serverLogCleared;

    private void WriteLogFile(string message) => WriteLog(IsServer, message);
    public static void WriteLog(bool isServer, string message)
    {
        var fileName = isServer ? "PosErrorLog_Server.txt" : "PosErrorLog_Client.txt";
        var path = System.IO.Path.Combine(Application.persistentDataPath, fileName);

        if (isServer && !serverLogCleared)
        {
            System.IO.File.WriteAllText(path, "");
            serverLogCleared = true;
        }
        else if (!isServer && !clientLogCleared)
        {
            System.IO.File.WriteAllText(path, "");
            clientLogCleared = true;
        }

        System.IO.File.AppendAllText(path, message + "\n");
    }

    public override void OnNetworkSpawn()
    {
        movementController = GetComponent<Character_Movement>();
        serverTransformData.OnValueChanged += OnServerTransformDataChanged;
    }

    public void RecordTickState(int tick, Vector2 moveDir, Vector3 worldMoveDir, Vector3 lookDir, MovementState movementState,
        bool jumpPressed, Vector3 velocity, Force_Movement currentForceMovement)
    {
        replayStates[tick % BUFFERSIZE] = new ReplayTickState
        {
            tick = tick,
            moveDir = moveDir,
            worldMoveDir = worldMoveDir,
            lookDir = lookDir,
            movementState = movementState,
            jumpPressed = jumpPressed,
            velocity = velocity,
            hadForcedMovement = currentForceMovement != null,
            forcedMovementTimer = currentForceMovement?.GetTimer() ?? 0f
        };
    }

    public override void OnNetworkDespawn()
    {
        serverTransformData.OnValueChanged -= OnServerTransformDataChanged;
    }

    public void ProcessTick(int tick)
    {
        currentTick = tick;
        SyncTransform();
    }

    private void SyncTransform()
    {
        if (IsServer)
        {
            var simulatedTick = movementController != null ? movementController.GetSimulatedTick() : currentTick;

            serverTransformData.Value = new SyncedTransformData
            {
                tick = simulatedTick,
                rotation = transform.rotation,
                position = transform.position
            };

            if (logPositionError && movementController != null && !NetworkObject.IsOwnedByServer)
            {
                var msg = $"[PosError-Server] netId={NetworkObjectId} name={gameObject.name} tick={simulatedTick} " +
                           $"pos=({transform.position.x:F4},{transform.position.y:F4},{transform.position.z:F4}) " +
                           $"rotY={transform.rotation.eulerAngles.y:F2} " +
                           $"speed={movementController.GetSpeed():F4} baseSpeed={movementController.GetBaseSpeed():F4} speedMultiplier={movementController.GetSpeedMultiplier():F4} " +
                           $"velocity=({movementController.Velocity.x:F4},{movementController.Velocity.y:F4},{movementController.Velocity.z:F4})";
                Debug.Log(msg);
                WriteLogFile(msg);
            }

            if (logRotation && !NetworkObject.IsOwnedByServer)
            {
                var rot = transform.rotation.eulerAngles;
                var rotMsg = $"[RotationSend-Server] netId={NetworkObjectId} name={gameObject.name} tick={currentTick} " +
                             $"rot=({rot.x:F2},{rot.y:F2},{rot.z:F2})";
                Debug.Log(rotMsg);
                WriteLogFile(rotMsg);
            }
        }
        else if (NetworkObject.IsOwner)
        {
            var clientTransformData = new SyncedTransformData
            {
                tick = currentTick,
                rotation = transform.rotation,
                position = transform.position
            };
            clientMovementDatas[currentTick % BUFFERSIZE] = clientTransformData;

            if (logRotation)
            {
                var rot = transform.rotation.eulerAngles;
                var rotMsg = $"[RotationSend] netId={NetworkObjectId} name={gameObject.name} tick={currentTick} " +
                             $"rot=({rot.x:F2},{rot.y:F2},{rot.z:F2})";
                Debug.Log(rotMsg);
                WriteLogFile(rotMsg);
            }
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, ServerPosition, positionLerpSpeed * Time.deltaTime);
            transform.rotation =
                Quaternion.Lerp(transform.rotation, ServerRotation, rotationLerpSpeed * Time.deltaTime);
        }
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (!IsServer) return;

        transform.SetPositionAndRotation(position, rotation);
        serverTransformData.Value = new SyncedTransformData
        {
            tick = currentTick,
            position = position,
            rotation = rotation
        };

        TeleportOwnerClientRpc(position, rotation, currentTick);
    }

    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 position, Quaternion rotation, int tick)
    {
        if (IsServer || !NetworkObject.IsOwner) return;

        transform.SetPositionAndRotation(position, rotation);
        clientMovementDatas[tick % BUFFERSIZE] = new SyncedTransformData
        {
            tick = tick,
            position = position,
            rotation = rotation
        };
        posErrorCount = 0;
    }

    private void OnServerTransformDataChanged(SyncedTransformData previousvalue, SyncedTransformData serverState)
    {
        if (!NetworkObject.IsOwner || IsServer) return;
        
        Debug.Log("[ServerDataChange] localTick: " + tickSystem.LocalTime.Tick + " serverTick: " + tickSystem.ServerTime.Tick + " serverStateTick: " + serverState.tick);

        var serverTick = serverState.tick;
        var clientState = clientMovementDatas[serverTick % BUFFERSIZE];

        if (clientState == null || clientState.tick != serverTick) 
            return;

        HandlePositionError(serverState, clientState, serverTick);
    }

    private void HandlePositionError(SyncedTransformData serverState, SyncedTransformData clientState, int localTick)
    {
        var diffPosition = clientState.position - serverState.position;

        if (logPositionError)
            LogPositionError(localTick, diffPosition, clientState.position, serverState.position, clientState.rotation, serverState.rotation);

        if (diffPosition.magnitude > hardResetDistance)
        {
            HardReset(serverState);
            return;
        }

        if (diffPosition.magnitude > positionError)
        {
            posErrorCount++;
            if (posErrorCount > errorTickThreshold)
            {
                ReplayFrom(localTick, serverState, diffPosition);
                posErrorCount = 0;
            }
        }
        else
        {
            posErrorCount = 0;
            var currentPos = transform.position;
            transform.position = Vector3.Lerp(currentPos, currentPos - diffPosition, positionErrorPassiveFix);
        }
    }


    private void HardReset(SyncedTransformData serverState)
    {
        var resetMsg = $"[PosError-HardReset] netId={NetworkObjectId} name={gameObject.name} tick={currentTick} " +
                       $"snapTo=({serverState.position.x:F4},{serverState.position.y:F4},{serverState.position.z:F4})";
        Debug.Log(resetMsg);
        if (logPositionError)
            WriteLogFile(resetMsg);

        transform.SetPositionAndRotation(serverState.position, serverState.rotation);

        for (var i = 0; i < BUFFERSIZE; i++)
        {
            clientMovementDatas[i] = null;
            replayStates[i] = null;
        }

        clientMovementDatas[currentTick % BUFFERSIZE] = new SyncedTransformData
        {
            tick = currentTick,
            position = serverState.position,
            rotation = serverState.rotation
        };

        posErrorCount = 0;
    }

    private void LogPositionError(int localTick, Vector3 diffPosition, Vector3 clientPos, Vector3 serverPos,
        Quaternion clientRot, Quaternion serverRot)
    {
        var replay = replayStates[localTick % BUFFERSIZE];
        var moveDir = (replay != null && replay.tick == localTick) ? replay.moveDir : Vector2.zero;
        var worldMoveDir = (replay != null && replay.tick == localTick) ? replay.worldMoveDir : Vector3.zero;
        var lookDir = (replay != null && replay.tick == localTick) ? replay.lookDir : Vector3.zero;
        var grounded = movementController != null && movementController.IsGrounded();
        var forcedMovement = movementController != null && movementController.CurrentForceMovement != null;
        var speed = movementController != null ? movementController.GetSpeed() : -1f;
        var baseSpeed = movementController != null ? movementController.GetBaseSpeed() : -1f;
        var speedMultiplier = movementController != null ? movementController.GetSpeedMultiplier() : -1f;
        var velocity = movementController != null ? movementController.Velocity : Vector3.zero;
        var rotDiffDeg = Quaternion.Angle(clientRot, serverRot);

        var msg = $"[PosError] netId={NetworkObjectId} name={gameObject.name} tick={localTick} " +
                   $"clientPos=({clientPos.x:F4},{clientPos.y:F4},{clientPos.z:F4}) " +
                   $"serverPos=({serverPos.x:F4},{serverPos.y:F4},{serverPos.z:F4}) " +
                   $"diffX={diffPosition.x:F4} diffY={diffPosition.y:F4} diffZ={diffPosition.z:F4} diffMag={diffPosition.magnitude:F4} " +
                   $"clientRotY={clientRot.eulerAngles.y:F2} serverRotY={serverRot.eulerAngles.y:F2} rotDiffDeg={rotDiffDeg:F2} " +
                   $"moveDir=({moveDir.x:F2},{moveDir.y:F2}) worldMoveDir=({worldMoveDir.x:F2},{worldMoveDir.y:F2},{worldMoveDir.z:F2}) lookDir=({lookDir.x:F2},{lookDir.y:F2},{lookDir.z:F2}) grounded={grounded} forcedMovement={forcedMovement} " +
                   $"speed={speed:F4} baseSpeed={baseSpeed:F4} speedMultiplier={speedMultiplier:F4} velocity=({velocity.x:F4},{velocity.y:F4},{velocity.z:F4})";
        Debug.Log(msg);
        WriteLogFile(msg);
    }

    private void ReplayFrom(int localTick, SyncedTransformData serverState, Vector3 diffPosition)
    {
        var replayEntry = replayStates[localTick % BUFFERSIZE];
        if (replayEntry == null || replayEntry.tick != localTick || movementController == null)
        {
            var fallbackMsg = $"[PosError-Correction-Fallback] netId={NetworkObjectId} name={gameObject.name} " +
                               $"localTick={localTick} currentTick={currentTick} diffMag={diffPosition.magnitude:F4} " +
                               $"reason={(replayEntry == null ? "no-entry" : replayEntry.tick != localTick ? "tick-mismatch" : "no-movementController")}";
            Debug.Log(fallbackMsg);
            if (logPositionError)
                WriteLogFile(fallbackMsg);

            FlatPatchFrom(localTick, diffPosition);
            return;
        }

        var replayMsg = $"[PosError-Correction] netId={NetworkObjectId} name={gameObject.name} " +
                         $"replaying from tick {localTick} to {currentTick} ({currentTick - localTick} ticks) " +
                         $"diffMag={diffPosition.magnitude:F4}";
        Debug.Log(replayMsg);
        if (logPositionError)
            WriteLogFile(replayMsg);

        transform.SetPositionAndRotation(serverState.position, serverState.rotation);
        movementController.RestoreVelocity(replayEntry.velocity);
        if (replayEntry.hadForcedMovement && movementController.CurrentForceMovement != null)
        {
            movementController.CurrentForceMovement.RestoreTimer(replayEntry.forcedMovementTimer);
            movementController.CurrentForceMovement.RestoreVelocity(replayEntry.velocity);
        }

        clientMovementDatas[localTick % BUFFERSIZE] = serverState;

        var index = localTick + 1;
        while (index <= currentTick)
        {
            var replay = replayStates[index % BUFFERSIZE];
            if (replay == null || replay.tick != index)
                break; // buffer gap (e.g. wrapped around) - best effort stops here.

            movementController.SimulateTick(replay.moveDir, replay.worldMoveDir, replay.lookDir, replay.movementState, replay.jumpPressed);

            clientMovementDatas[index % BUFFERSIZE] = new SyncedTransformData
            {
                tick = index,
                position = transform.position,
                rotation = transform.rotation
            };

            replay.velocity = movementController.Velocity;
            replay.hadForcedMovement = movementController.CurrentForceMovement != null;
            replay.forcedMovementTimer = movementController.CurrentForceMovement?.GetTimer() ?? 0f;

            index++;
        }
    }

    private void FlatPatchFrom(int localTick, Vector3 diffPosition)
    {
        var index = localTick;
        var entry = clientMovementDatas[index % BUFFERSIZE];
        if (entry == null || index != entry.tick)
            return;

        while (index <= currentTick)
        {
            var state = clientMovementDatas[index % BUFFERSIZE];
            clientMovementDatas[index % BUFFERSIZE] = new SyncedTransformData
            {
                tick = state.tick,
                rotation = state.rotation,
                position = state.position - diffPosition
            };

            index++;
        }

        transform.position = clientMovementDatas[currentTick % BUFFERSIZE].position;
    }
}


public class ReplayTickState
{
    public int tick;
    public Vector2 moveDir;
    public Vector3 worldMoveDir;
    public Vector3 lookDir;
    public MovementState movementState;
    public bool jumpPressed;
    public Vector3 velocity;
    public bool hadForcedMovement;
    public float forcedMovementTimer;
}

[System.Serializable]
public class SyncedTransformData : INetworkSerializable, IEquatable<SyncedTransformData>
{
    public int tick;
    public Quaternion rotation;
    public Vector3 position;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref tick);
        serializer.SerializeValue(ref rotation);
        serializer.SerializeValue(ref position);
    }

    public bool Equals(SyncedTransformData other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return tick == other.tick && rotation.Equals(other.rotation) && position.Equals(other.position);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SyncedTransformData)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(tick, rotation, position);
    }
}