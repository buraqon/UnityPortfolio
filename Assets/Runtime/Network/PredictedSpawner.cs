using System;
using System.Collections;
using System.Collections.Generic;
using HippoLib;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PredictedSpawner : NetworkBehaviour
{
    public static PredictedSpawner Instance;
    // Queue<PredictedSpawn> spawnQueue = new Queue<PredictedSpawn>();
    Dictionary< int,PredictedSpawn> predictedSpawns = new Dictionary<int, PredictedSpawn>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Debug.LogWarning("Multiple instances of spawner");
    }

    public T Spawn<T>(T original, Vector3 position, Quaternion rotation, NetworkObject caller, Action<T> action = null) where T : PredictedSpawn
    {
        var spawnable = Instantiate(original, position, rotation);
        action?.Invoke(spawnable);
        if(!IsServer && caller.IsOwner)
        {
            spawnable.OnNetworkSpawn();
            spawnable.OnPredictedSpawn();
            // spawnQueue.Enqueue(spawnable);
        }
        if (IsServer)
        {
            spawnable.NetworkObject.SpawnWithOwnership(caller.OwnerClientId);
        }
        return spawnable;
    }

    //public void OnNetworkedObjectSpawned(PredictedSpawn spawn)
    //{
    //    var id = spawn.GetInstanceID();
    //    var predictedItem = spawnQueue.Dequeue();

    //    predictedSpawns.Add(id, predictedItem);
    //    spawn.OnDestroyed += () =>
    //    {
    //        var spawned = predictedSpawns[id];
    //        predictedSpawns.Remove(id);
    //        Destroy(spawned.gameObject);
    //    };
    //}
}
