using UnityEngine;
using HippoLib.Pooling;
using System;
using Unity.Netcode.Components;

namespace HippoLib
{
    [RequireComponent(typeof(Pool_Item))]
    public class Conjure : PredictedSpawn
    {
        public IConjureSender Caster { get; private set; }

        private Conjure_Data data;
        private bool isActive;
        private bool isDying;
        private float deathTimer;
        
        protected TargetType targetType = TargetType.Enemy;

        public Action OnSpawned;
        public Action<IConjureReciever> OnTrigger;
        public Action OnEndLife;


        protected virtual void ConjureSpawned()
        {
        }

        protected virtual void ConjureUpdate()
        {
        }


        protected virtual void ConjureEndLife()
        {
        }

        protected virtual void InActiveUpdate()
        {
        }


        public void Initialize(Conjure_Data conjure_Data, Conjure_Params parameters, TargetType targetType)
        {
            data = conjure_Data;
            Caster = parameters.Caster;
            this.targetType = targetType;
        }

        public override void OnLocalSpawn()
        {
            var netTransform = GetComponent<AnticipatedNetworkTransform>();
            if (netTransform)
            {
                Destroy(netTransform);
            }
        }

        public override void OnPredictedSpawn()
        {
            if (!Owned) return;

            ConjureSpawned();
            OnSpawned?.Invoke();
            isActive = true;
            isDying = false;
            deathTimer = 0;
        }

        public void Update()
        {
            if ((Owned) && isActive)
                ConjureUpdate();

            if (isDying)
                DeathUpdate();
        }

        public void EndLife()
        {
            if (!Owned || !isActive) return;

            ConjureEndLife();
            OnEndLife?.Invoke();

            if (data.ChainedConjure != null)
            {
                var param = new Conjure_Params(Caster, transform.position, transform.rotation);
                data.ChainedConjure.SpawnSpell(param);
            }

            isActive = false;
            isDying = true;
        }

        private void DeathUpdate()
        {
            deathTimer += Time.deltaTime;
            if (deathTimer >= data.LingerTime)
                Despawn();
        }

        // protected void Despawn()
        // {
        //     if(!IsSpawned)
        //         Destroy(gameObject);
        //     else
        //         NetworkObject.Despawn();
        // }

        protected virtual bool IsTarget(IConjureSender sender, IConjureReciever reciever)
        {
            if (!reciever.IsAlive()) return false;

            if (ReferenceEquals(sender, reciever))
                return targetType.HasFlag(TargetType.Self);

            if (sender.IsTarget(reciever))
                return targetType.HasFlag(TargetType.Enemy);

            return targetType.HasFlag(TargetType.Ally);
        }

        protected void TriggerOnReciever(IConjureReciever reciever)
        {
            OnTrigger?.Invoke(reciever);
        }
        
        public float GetDamageMultiplier()
        {
            return data.DamageMultiplier;
        }
    }

    [Flags]
    public enum TargetType
    {
        None = 0,
        Enemy = 1 << 0,
        Ally = 1 << 1,
        Self = 1 << 2
    }

    public class Conjure_Params
    {
        public IConjureSender Caster;

        public Vector3 Position = Vector3.forward;
        public Quaternion Rotation = Quaternion.identity;

        public Conjure_Params(IConjureSender caster, Vector3 spawnPos, Quaternion spawnRotation)
        {
            Caster = caster;

            Position = spawnPos;
            Rotation = spawnRotation;
        }
    }
}