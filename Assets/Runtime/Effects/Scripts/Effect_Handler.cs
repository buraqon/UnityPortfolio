using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

namespace HippoLib.Effects
{
    using Effector;

    public abstract class Effect_Handler<TSender, TReciever> : NetworkBehaviour
        where TSender : IEffectSender where TReciever : IEffectReciever
    {
        protected TReciever _reciever;

        // [SerializeField] private Effect_DataList<TSender, TReciever> CurrentEffects = new ();
        private List<Effect_Effector<TSender, TReciever>> currentEffectors = new List<Effect_Effector<TSender, TReciever>>();
        
        public Action <Effect_Data<TSender, TReciever>, TSender> OnEffectAdded;
        public Action <Effect_Data<TSender, TReciever>> OnEffectRemoved;

        public List<Effect_Effector<TSender, TReciever>> CurrentEffectors => currentEffectors;
        
        public void Initialize()
        {
            // CurrentEffects.OnValueAdded += (obj) => OnEffectorAdded?.Invoke(obj);
            // CurrentEffects.OnValueRemoved += (obj) => OnEffectorRemoved?.Invoke(obj);
            // CurrentEffects.InitializeNetworkList();

            _reciever = GetComponent<TReciever>();
        }

        public void OnServerUpdate()
        {
            RemoveOldEffectors();
            StartNewEffectors();
            UpdateEffectors();
        }

        private void RemoveOldEffectors()
        {
            for (int i = currentEffectors.Count - 1; i >= 0; i--)
            {
                if (currentEffectors[i].IsDone)
                {
                    var effectorToRemove = currentEffectors[i];
                    effectorToRemove.OnRemove(_reciever);
                    currentEffectors.RemoveAt(i);
                    OnEffectRemoved?.Invoke(effectorToRemove.GetData());
                }
            }
        }

        private void StartNewEffectors()
        {
            foreach (var effector in currentEffectors)
            {
                if (effector.IsNew)
                    effector.OnAdd(_reciever);
            }
        }

        private void UpdateEffectors()
        {
            foreach (var effector in currentEffectors)
            {
                effector.UpdateEffector(_reciever);
            }
        }

        public Effect_Effector<TSender, TReciever> AddNewEffector(TSender caster,
            Effect_Data<TSender, TReciever> effect)
        {
            var effector = effect.InstantiateEffector(caster);
            currentEffectors.Add(effector);
            OnEffectAdded?.Invoke(effect, caster);
            return effector;
        }

        public void AddNewEffector(TSender caster, List<Effect_Data<TSender, TReciever>> effects)
        {
            effects.ForEach(x => AddNewEffector(caster, x));
        }

        public void OnCharacterDied()
        {
            foreach (var effector in currentEffectors)
            {
                effector.ForceFinish();
            }
        }
    }
}