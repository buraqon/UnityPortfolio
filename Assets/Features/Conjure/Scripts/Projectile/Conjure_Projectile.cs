using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HippoLib.Conjures
{
    public class Conjure_Projectile : Conjure
    {
        [HideInInspector] public float Radius = 0.5f;
        [HideInInspector] public float Speed;
        [HideInInspector] public float Range;
        [HideInInspector] public int PierceCount;
        [HideInInspector] public float RangeMultiplier = 1;

        public Action<IConjureReciever> OnHit;
        public Action OnMiss;

        private float lifetime;
        private float timer = 0;

        private List<IConjureReciever> alreadyHit = new();

        protected override void ConjureSpawned()
        {
        }

        public void InitProjectile(Conjure_Data_Projectile data)
        {
            Speed = data.Speed;
            Range = data.Range;
            Radius = data.Radius;
            PierceCount = data.PierceCount;
            RangeMultiplier = data.RangeMultiplier;

            lifetime = Range * RangeMultiplier / Speed;
            timer = 0;
        }

        protected override void ConjureUpdate()
        {
            if (!(Owned)) return;

            timer += Caster.DeltaTime;
            MoveProjectile();
            CheckForTargetCollision();
            if (IsDone())
            {
                if (alreadyHit.Count <= 0)
                    OnMiss?.Invoke();

                EndLife();
            }
        }

        protected virtual void MoveProjectile()
        {
            transform.position += transform.forward * Speed * Caster.DeltaTime;
        }

        private void CheckForTargetCollision()
        {
            var target = GetTargetFromCollider();
            if (target != null)
            {
                OnTargetHit(target);
            }
        }

        protected virtual void OnTargetHit(IConjureReciever target)
        {
            if (alreadyHit.Contains(target))
                return;

            TriggerOnReciever(target);
            alreadyHit.Add(target);
            OnHit?.Invoke(target);

            if (PierceCount >= 0)
            {
                PierceCount--;
                if (PierceCount <= 0)
                    EndLife();
            }
        }

        private IConjureReciever GetTargetFromCollider()
        {
            var colliders = Physics.OverlapSphere(transform.position, Radius);
            foreach (var collider in colliders)
            {
                var reciever = collider.GetComponent<IConjureReciever>();
                if (reciever != null && IsTarget(Caster, reciever))
                    return reciever;
            }

            return null;
        }

        protected virtual bool IsDone()
        {
            return timer > lifetime;
        }
    }
}