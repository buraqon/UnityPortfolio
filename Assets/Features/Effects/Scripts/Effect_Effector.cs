using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.Effects.Effector
{

    public abstract class Effect_Effector<TSender, TReciever> where TSender : IEffectSender where TReciever : IEffectReciever
    {
        public bool IsDone { get; private set; }
        public bool IsNew { get; private set; }

        private Effect_Data<TSender, TReciever> _data;
        private TSender _caster;

        protected virtual void OnAdded(TReciever reciever) { }
        protected virtual void OnUpdate(TReciever reciever, float deltaTime) { }
        protected virtual void OnRemoved() { }

        protected virtual void OnDamaged() { }

        public void OnInstantiate(Effect_Data<TSender, TReciever> effects_Data, TSender caster)
        {
            _data = effects_Data;
            _caster = caster;
            IsNew = true;
            IsDone = false;
        }

        public void Finished()
        {
            IsDone = true;
        }

        public void ForceFinish()
        {
            IsDone = true;
        }

        public void OnAdd(TReciever reciever)
        {
            IsNew = false;
            _data.Added(_caster, reciever);
            OnAdded(reciever);
        }

        public void UpdateEffector(TReciever reciever)
        {
            _data.UpdateEffect(_caster, reciever);
            OnUpdate(reciever, reciever.DeltaTime);
        }
        
        protected void TriggerEffect(TReciever reciever)
        {
            _data.TriggerEffect(_caster, reciever);
        }

        public void OnRemove(TReciever reciever)
        {
            _data.Removed(_caster, reciever);
            OnRemoved();
        }


        
        public Effect_Data<TSender, TReciever> GetData()
        {
            return _data;
        }
    }
}