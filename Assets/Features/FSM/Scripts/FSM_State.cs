using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiniteStateMachine
{
    public abstract class FSM_State : ScriptableObject
    {
        [HideInInspector] public FSM_TransitionList transitions;
        [HideInInspector] public string guid;
        protected IFSMUser user;

#if UNITY_EDITOR
        [HideInInspector] public Vector2 graphPosition;
#endif


        public abstract void StateEnter();

        public void StateUpdate(float deltaTime)
        {
            OnStateUpdate(deltaTime);
            transitions.Update(deltaTime);
        }

        protected abstract void OnStateUpdate(float deltaTime);
        public abstract void StateExit();

        public FSM_State GetNextState()
        {
            if (IsReadyToLeave())
                return transitions.GetNextState();
            
            return null;
        }
        
        protected virtual bool IsReadyToLeave()
        {
            return true;
        }

        public virtual bool IsReadyToStart()
        {
            return true;
        }

        public FSM_State InstantiateState()
        {
            var stateInstant = Instantiate(this);
            stateInstant.transitions = transitions.InstantiateTransitionlist();
            OnInstantiate(stateInstant);
            return stateInstant;
        }

        protected abstract void OnInstantiate(FSM_State stateInstant);

        public virtual void AfterInstantiate()
        {
        }
        
        public void SetUser(IFSMUser user)
        {
            this.user = user;
            transitions.SetUser(user);
            OnSetUser(user);
        }

        protected abstract void OnSetUser(IFSMUser user);



#if UNITY_EDITOR
        public void AddTransitionTo(FSM_State toState)
        {
            transitions.AddTransition(toState);
        }

        public void RemoveTransitionTo(FSM_State toState)
        {
            transitions.RemoveTransition(toState);
        }
#endif
   
    }
}