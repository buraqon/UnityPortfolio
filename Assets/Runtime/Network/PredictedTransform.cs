using System;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class PredictedTransform : NetworkBehaviour
{
    [Header("Position Networking")] [SerializeField]
    private float positionError = 1f;

    [SerializeField] private float positionErrorPassiveFix = 0.05f;
    [SerializeField] private float positionLerpSpeed = 10f;
    [SerializeField] private int errorTickThreshold = 10;

    [Header("Rotation Networking")] [SerializeField]
    private float rotationError = 20f;

    [SerializeField] private float rotationLerpSpeed = 10f;

    [Header("Tick Networking")] [SerializeField]
    private SyncedTransformData[] clientMovementDatas = new SyncedTransformData[BUFFERSIZE];

    [SerializeField] private NetworkVariable<SyncedTransformData> serverTransformData = new();
    [SerializeField] int currentTick;

    private NetworkTickSystem tickSystem => NetworkManager.Singleton.NetworkTickSystem;
    private const int BUFFERSIZE = 1024;
    private int posErrorCount;
    private bool isActive = true;

    public Vector3 ServerPosition => serverTransformData.Value.position;
    public Quaternion ServerRotation => serverTransformData.Value.rotation;

    public override void OnNetworkSpawn()
    {
        serverTransformData.OnValueChanged += OnServerTransformDataChanged;
        tickSystem.Tick += OnTick;
    }

    private void OnTick()
    {
        if (isActive)
        {
            currentTick = tickSystem.LocalTime.Tick;
            SyncTransform();
        }
    }

    private void SyncTransform()
    {
        if (IsServer)
        {
            serverTransformData.Value = new SyncedTransformData
            {
                tick = currentTick,
                rotation = transform.rotation,
                position = transform.position
            };
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
        }
        else
        {
            LerpToServer();
        }
    }

    private void LerpToServer()
    {
        transform.position = Vector3.Lerp(transform.position, ServerPosition, positionLerpSpeed * Time.deltaTime);
        transform.rotation =
            Quaternion.Lerp(transform.rotation, ServerRotation, rotationLerpSpeed * Time.deltaTime);
    }

    private void OnServerTransformDataChanged(SyncedTransformData previousvalue, SyncedTransformData serverState)
    {
        if (!NetworkObject.IsOwner || IsServer)
            return;


        if (isActive)
        {
            var clientState = clientMovementDatas[serverState.tick % BUFFERSIZE];
            HandlePositionError(serverState, clientState);
        }
        else
        {
            LerpToServer();
        }
    }

    private void HandlePositionError(SyncedTransformData serverState, SyncedTransformData clientState)
    {
        var diffPosition = clientState.position - serverState.position;
        if (diffPosition.magnitude > positionError)
        {
            posErrorCount++;
            if (posErrorCount > errorTickThreshold)
            {
                CorrectPositionError(serverState, diffPosition);
                posErrorCount = 0;
            }
        }
        else
        {
            var currentPos = transform.position;
            transform.position = Vector3.Lerp(currentPos, currentPos - diffPosition, positionErrorPassiveFix);
        }
    }

    private void CorrectPositionError(SyncedTransformData serverState, Vector3 diffPosition)
    {
        var index = serverState.tick;
        if (index != clientMovementDatas[index % BUFFERSIZE].tick)
            return;

        Debug.Log($"Position Error: {diffPosition.magnitude} at tick {serverState.tick}");
        Debug.Log("Client first tick " + clientMovementDatas[index % BUFFERSIZE].tick);

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

    private Vector3 GetNeededVector(bool3 variables, Vector3 source, Vector3 destination)
    {
        if (!variables.x) destination.x = source.x;
        if (!variables.y) destination.y = source.y;
        if (!variables.z) destination.z = source.z;
        return destination;
    }

    private Quaternion GetNeededQuaternion(bool3 variables, Quaternion source, Quaternion destination)
    {
        Vector3 sourceEuler = source.eulerAngles;
        Vector3 destinationEuler = destination.eulerAngles;

        if (!variables.x) destinationEuler.x = sourceEuler.x;
        if (!variables.y) destinationEuler.y = sourceEuler.y;
        if (!variables.z) destinationEuler.z = sourceEuler.z;

        return Quaternion.Euler(destinationEuler);
    }

    public void EnablePrediction()
    {
        isActive = true;
    }

    public void DisablePrediction()
    {
        isActive = false;
    }
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