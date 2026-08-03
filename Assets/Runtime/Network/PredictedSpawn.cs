using System;
using Unity.Netcode;

public class PredictedSpawn : NetworkBehaviour
{
    // public Action OnDestroyed;
    public bool Owned => IsOwner || IsServer || !IsSpawned;

    
    public override void OnNetworkSpawn()
    {
        if (!IsServer && IsOwner && IsSpawned)
        {
            gameObject.SetActive(false);
        }
        else
        {
            OnPredictedSpawn();
        }

        if (!IsSpawned)
        {
            OnLocalSpawn();
        }
    }
    
    public virtual void OnPredictedSpawn(){}
    public virtual void OnLocalSpawn() {}
    
    // public override void OnNetworkDespawn()
    // {
    //     // OnDestroyed?.Invoke();
    //     base.OnDestroy();
    // }

    protected void Despawn()
    {
        if(!IsSpawned)
            Destroy(gameObject);
        else
            NetworkObject.Despawn();
    }
}
