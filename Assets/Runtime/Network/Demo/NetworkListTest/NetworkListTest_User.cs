using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkListTest_User : NetworkBehaviour
{
    public NetworkList<NetworkedEffectData> EffectList = new NetworkList<NetworkedEffectData>();

    private void Start()
    {
        EffectList.OnListChanged += OnEffectListChanged;
    }

    private void OnEffectListChanged(NetworkListEvent<NetworkedEffectData> changeevent)
    {
        Debug.Log("Changed Event: " + changeevent.Type + " - " + changeevent.Value.EffectID + " - " + changeevent.Value.SourceNetworkObjectId + " - " + changeevent.Value.TimeStamp);
    }

    private void Update()
    {
        if(IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var effectData = new NetworkedEffectData
                {
                    EffectID = UnityEngine.Random.Range(0, 10),
                    SourceNetworkObjectId = NetworkObject.NetworkObjectId,
                    TimeStamp = Time.time
                };
                EffectList.Add(effectData);
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
               EffectList.RemoveAt(0);
               var effectData = new NetworkedEffectData
               {
                   EffectID = UnityEngine.Random.Range(0, 10),
                   SourceNetworkObjectId = NetworkObject.NetworkObjectId,
                   TimeStamp = Time.time
               };
               EffectList.Insert(0, effectData);
            }
        }
        
        Debug.Log(OwnerClientId + " - EffectList Count: " + EffectList.Count);
        foreach (var effect in EffectList)
        {
            Debug.Log(effect.EffectID + " - " + effect.SourceNetworkObjectId + " - " + effect.TimeStamp);
        }
        
        Debug.Log("_________________________");
    }
}


public struct NetworkedEffectData : INetworkSerializable, IEquatable<NetworkedEffectData>
{
    public int EffectID;
    public ulong SourceNetworkObjectId;
    public float TimeStamp;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref EffectID);
        serializer.SerializeValue(ref SourceNetworkObjectId);
        serializer.SerializeValue(ref TimeStamp);
    }

    public bool Equals(NetworkedEffectData other)
    {
        return EffectID == other.EffectID && SourceNetworkObjectId == other.SourceNetworkObjectId && TimeStamp.Equals(other.TimeStamp);
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkedEffectData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EffectID, SourceNetworkObjectId, TimeStamp);
    }
}