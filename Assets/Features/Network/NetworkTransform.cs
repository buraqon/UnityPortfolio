using System;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkTransform : NetworkBehaviour
{
    [Header("Position Networking")]
    [SerializeField] private float positionLerpSpeed = 10f;

    [Header("Rotation Networking")] 
    [SerializeField] private float rotationLerpSpeed = 10f;

    [SerializeField] private NetworkVariable<SyncedTransformData> serverTransformData = new();
    [SerializeField] int currentTick;


    public Vector3 ServerPosition => serverTransformData.Value.position;
    public Quaternion ServerRotation => serverTransformData.Value.rotation;

    public override void OnNetworkSpawn()
    {
        serverTransformData.OnValueChanged += OnServerTransformDataChanged;
    }

    private void Update()
    {
        SyncTransform();
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
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, ServerPosition, positionLerpSpeed * Time.deltaTime);
            transform.rotation =
                Quaternion.Lerp(transform.rotation, ServerRotation, rotationLerpSpeed * Time.deltaTime);
        }
    }

    private void OnServerTransformDataChanged(SyncedTransformData previousvalue, SyncedTransformData serverState)
    {
        if (!NetworkObject.IsOwner || IsServer) return;

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
}
