using System;
using Unity.Netcode;
using UnityEngine;

namespace HippoLib.Effects
{
      public class Effect_Networker<TSender, TReciever> : NetworkBehaviour 
        where TSender : IEffectSender where TReciever : IEffectReciever
    {
        [SerializeField] private Effect_Handler<TSender, TReciever> localEffectorHolder;
        private NetworkList<NetworkedEffectData> networkedEffects = new NetworkList<NetworkedEffectData>();

        public override void OnNetworkSpawn()
        {
            networkedEffects.OnListChanged += OnNetworkedEffectsChanged;
            
            if (!IsServer && !IsOwner) return;
            
            if (localEffectorHolder != null)
            {
                localEffectorHolder.OnEffectAdded += SyncEffectAdd;
                localEffectorHolder.OnEffectRemoved += SyncEffectRemove;
            }
        }

        private void SyncEffectAdd(Effect_Data<TSender, TReciever> effectData, TSender source)
        {
            if (!IsServer)
                return;

            var networkEffect = new NetworkedEffectData
            {
                EffectID = Effect_Database<TSender, TReciever>.Instance.GetIndexFromData(effectData),
                SourceNetworkObjectId = source.NetworkObject.NetworkObjectId,
            };

            Debug.Log($"Send effect {networkEffect.EffectID} from {source.transform.name}");
            
            networkedEffects.Add(networkEffect);
        }

        private void SyncEffectRemove(Effect_Data<TSender, TReciever> effectData)
        {
            if (!IsServer)
                return;

            for (int i = networkedEffects.Count - 1; i >= 0; i--)
            {
                var id = Effect_Database<TSender, TReciever>.Instance.GetIndexFromData(effectData);
                if (networkedEffects[i].EffectID == id)
                {
                    networkedEffects.RemoveAt(i);
                    break;
                }
            }
        }

        private void OnNetworkedEffectsChanged(NetworkListEvent<NetworkedEffectData> changeEvent)
        {
            if (IsServer) 
                return;

            switch (changeEvent.Type)
            {
                case NetworkListEvent<NetworkedEffectData>.EventType.Add:
                    HandleEffectAdd(changeEvent.Value);
                    break;
                case NetworkListEvent<NetworkedEffectData>.EventType.Remove:
                    // HandleEffectRemove(changeEvent.Index);
                    break;
            }
        }

        private void HandleEffectAdd(NetworkedEffectData networkEffect)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                networkEffect.SourceNetworkObjectId, out NetworkObject sourceObj))
            {
                Debug.LogWarning($"Could not find source object with ID {networkEffect.SourceNetworkObjectId}");
                return;
            }

            var source = sourceObj.GetComponent<TSender>();
            if (source == null)
            {
                Debug.LogWarning("Source object does not implement IEffectSource");
                return;
            }

            var effectData = Effect_Database<TSender, TReciever>.Instance.GetDataFromIndex(networkEffect.EffectID);
            if (effectData == null)
            {
                Debug.LogWarning($"Could not find effect data with ID {networkEffect.EffectID}");
                return;
            }
            
            Debug.Log($"Recieve effect {networkEffect.EffectID} from {source.transform.name}");

            localEffectorHolder.AddNewEffector(source, effectData);
        }

        // private void HandleEffectRemove(int index)
        // {
        //     var networkEffect = networkedEffects[index];
        //     foreach (var effector in localEffectorHolder.CurrentEffectors)
        //     {
        //         var id = Effect_Database<TSender, TReciever>.Instance.GetIndexFromData(effector.GetData());
        //         if (networkEffect.EffectID == id)
        //         {
        //             effector.Finished();
        //             break;
        //         }
        //     }
        // }
    }

    public struct NetworkedEffectData : INetworkSerializable, IEquatable<NetworkedEffectData>
    {
        public int EffectID;
        public ulong SourceNetworkObjectId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref EffectID);
            serializer.SerializeValue(ref SourceNetworkObjectId);
        }

        public bool Equals(NetworkedEffectData other)
        {
            return EffectID == other.EffectID && SourceNetworkObjectId == other.SourceNetworkObjectId;
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkedEffectData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EffectID, SourceNetworkObjectId);
        }
    }
}