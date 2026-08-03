using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.Netcode;

namespace HippoLib.Conjures
{
    public class Conjure_AOE : Conjure
    {
        private OverlapCollider overlapCollider;
        private float timer = 0;
        private float tickCounter = 0;
        private Conjure_Data_AOE aoeData;
        private bool isStarted = false;

        public Conjure_Data_AOE AoeData => aoeData;

        public void InitAOE(Conjure_Data_AOE data)
        {
            aoeData = data;
            transform.localScale = Vector3.one * data.Scale * data.RangeMultiplier;
            var colliders = GetComponentsInChildren<Collider>().ToList();
            overlapCollider = new OverlapCollider(transform, colliders);
            isStarted = false;
        }

        protected override void ConjureSpawned()
        {
            timer = aoeData.Lifetime / aoeData.Ticks - aoeData.Delay;
            tickCounter = 0;
            isStarted = true;

            if (aoeData.IsSpawnedOnCharacter())
                transform.parent = Caster.transform;
        }

        protected override void ConjureUpdate()
        {
            if (!isStarted)
                return;

            timer += Caster.DeltaTime;
            if (timer > aoeData.Lifetime / aoeData.Ticks)
            {
                ApplyEffectOnTargets();
                timer = 0;
                tickCounter++;

                if (tickCounter >= aoeData.Ticks)
                {
                    EndLife();
                    return;
                }
            }

            if (aoeData.Following)
                transform.position = Caster.VisualTransform.position;
        }

        protected override void ConjureEndLife()
        {
        }

        protected virtual void ApplyEffectOnTargets()
        {
            // var colliders = Physics.OverlapSphere(transform.position, 100);
            // foreach (var collider in colliders)
            // {
            //     var reciever = collider.GetComponent<IConjureReciever>();
            //     if (reciever != null && IsTarget(Caster, reciever))
            //     {
            //         var isInShape = data.Shape.IsInsideShape(transform, reciever.transform.position);
            //         if (isInShape)
            //             TriggerOnReciever(reciever);
            //     }
            // }

            var colliderList = overlapCollider.ScanHitbox(transform.position, transform.rotation);
            foreach (var collid in colliderList)
            {
                var reciever = collid.GetComponent<IConjureReciever>();
                if (reciever != null && IsTarget(Caster, reciever))
                {
                    TriggerOnReciever(reciever);
                }
            }
        }
    }
}