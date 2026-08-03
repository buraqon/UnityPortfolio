using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.Effects
{
    using Effector;

    public abstract class Effect_Data<TSender, TReciever> : ScriptableObject
        where TSender : IEffectSender where TReciever : IEffectReciever
    {
        public string VisualName;
        public string Description;
        public float EffectMultiplier = 1;
        public Effect_Effector<TSender, TReciever> InstantiateEffector(TSender caster)
        {
            var effector = InstantiateEffector();
            effector.OnInstantiate(this, caster);
            return effector;
        }

        protected abstract Effect_Effector<TSender, TReciever> InstantiateEffector();

        public void Added(TSender caster, TReciever reciever)
        {
            OnAdded(caster, reciever);
        }

        protected virtual void OnAdded(TSender caster, TReciever reciever)
        {
        }

        public void UpdateEffect(TSender caster, TReciever reciever)
        {
            OnUpdateEffect(caster, reciever);
        }

        protected virtual void OnUpdateEffect(TSender caster, TReciever reciever)
        {
        }

        public void TriggerEffect(TSender caster, TReciever reciever)
        {
            OnTriggerEffect(caster, reciever);
        }

        protected virtual void OnTriggerEffect(TSender caster, TReciever reciever)
        {
        }

        public void Removed(TSender caster, TReciever reciever)
        {
            OnRemoved(caster, reciever);
        }

        protected virtual void OnRemoved(TSender caster, TReciever reciever)
        {
        }
    }
}