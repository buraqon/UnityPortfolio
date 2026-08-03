using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace FiniteStateMachine
{
    [System.Serializable]
    public abstract class FSM_Condition : ScriptableObject
    {
        [SerializeField] private FSM fsm;
        protected IFSMUser user;
        
        public FSM FSM => fsm;

        public abstract void OnStateEnter();
        public abstract bool EvaluateCondition();
        public abstract void UpdateCondition(float deltaTime);

        public FSM_Condition InstantiateCondition()
        {
            var instant = Instantiate(this);
            OnInstantiate(instant);
            return instant;
        }

        protected abstract void OnInstantiate(FSM_Condition instant);

        public void SetUser(IFSMUser user)
        {
            this.user = user;
            OnSetUser(user);
        }

        protected abstract void OnSetUser(IFSMUser user);

        public void SetFSM(FSM fsm)
        {
            this.fsm = fsm;
        }

#if UNITY_EDITOR
        public virtual void OnConditionDeleted()
        {
            
        }
#endif
    }
}
