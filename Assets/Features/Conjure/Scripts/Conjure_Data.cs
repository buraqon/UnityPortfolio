using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Netcode;

namespace HippoLib
{
    public abstract class Conjure_Data : ScriptableObject
    {
        [FormerlySerializedAs("Prefab")] [SerializeField]
        private Conjure conjurePrefab;

        [SerializeField] private TargetType targetType = TargetType.Enemy;

        [FormerlySerializedAs("IsLookAtAiming")] [SerializeField]
        private bool isLookAtAiming = true;

        [SerializeField] private Conjure_Data _chainedConjure;
        public float LingerTime = 1;

        public float DamageMultiplier = 1;
        public float RangeMultiplier = 1;

        public Conjure ConjurePrefab
        {
            get => conjurePrefab;
        }

        public bool IsLookAtAiming
        {
            get => isLookAtAiming;
        }

        public Conjure_Data ChainedConjure
        {
            get => _chainedConjure;
        }

        public Conjure SpawnSpell(Conjure_Params parameters)
        {
            var conjureSpawned = PredictedSpawner.Instance.Spawn(ConjurePrefab, parameters.Position,
                parameters.Rotation, parameters.Caster.NetworkObject, (conjure) =>
                {
                    conjure.Initialize(this, parameters, targetType);
                    OnSpawnSpell(conjure.NetworkObject);
                });

            return conjureSpawned;
        }


        protected virtual void OnSpawnSpell(NetworkObject obj)
        {
        }

        public virtual bool IsSpawnedOnCharacter()
        {
            return true;
        }

        public void SetDamageMultiplier(float multiplier)
        {
            DamageMultiplier = multiplier;
        }

        public void SetRangeMultiplier(float multiplier)
        {
            RangeMultiplier = multiplier;
        }
    }
}