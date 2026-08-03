using System;
using Unity.Netcode;
using UnityEngine;

public class DemoNPC_Spawner : NetworkBehaviour
{
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private GameObject npcTargetPrefab;
    [SerializeField] private float DistanceBetweenNPCs = 5;
     
    private int npcCount = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            RequestSpawnNPC();
    }

    private void RequestSpawnNPC()
    {
        if (IsServer)
            SpawnNPC();
        else
            SpawnNPCServerRPC();
    }

    private void SpawnNPC()
    {
        if(!IsServer)
            return;
        
        var spawnPos = npcCount % 2 == 0 ? new Vector3(npcCount/2, 0, 0) : new Vector3(-npcCount/2, 0, 0);
        npcCount++;
        var npc = Instantiate(npcPrefab, spawnPos, Quaternion.identity);
        npc.GetComponent<NetworkObject>().Spawn();
        
        var targetNpc = Instantiate(npcTargetPrefab, spawnPos + Vector3.forward * DistanceBetweenNPCs, Quaternion.identity);
        targetNpc.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc]
    private void SpawnNPCServerRPC()
    {
        SpawnNPC();
    }
}