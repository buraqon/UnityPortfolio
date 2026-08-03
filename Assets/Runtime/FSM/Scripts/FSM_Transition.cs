using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FiniteStateMachine
{
    [System.Serializable]
    public class FSM_Transition
    {
        public FSM_Condition Condition;
        public FSM_State NextState;

        public FSM_Transition(FSM_State toState)
        {
            NextState = toState;
        }

        public FSM_Transition(FSM_Condition condition, FSM_State nextState)
        {
            Condition = condition;
            NextState = nextState;
        }

        public FSM_Transition InstantiateTransition()
        {
            var conditionInsant = Condition?.InstantiateCondition();
            var instant = new FSM_Transition(conditionInsant, NextState);
            return instant;
        }

        public void OnStateEnter()
        {
            Condition?.OnStateEnter();
        }

        public void SetUser(IFSMUser user)
        {
            Condition?.SetUser(user);
        }
    }

    [System.Serializable]
    public class FSM_TransitionList
    {
        public List<FSM_Transition> Transitions = new List<FSM_Transition>();
        public FSM_State GetNextState()
        {
            foreach (var transition in Transitions)
            {
                var isReady = IsReadyToTransition(transition);
                if (isReady)
                    return transition.NextState;
            }

            return null;
        }
        
        private bool IsReadyToTransition(FSM_Transition transition)
        {
            if (!transition.NextState.IsReadyToStart())
                return false;
            
            if(!transition.Condition || transition.Condition.EvaluateCondition())
                return true; 

            return false;
        }
        
        public FSM_TransitionList InstantiateTransitionlist()
        {
            var instant = new FSM_TransitionList();
            instant.Transitions = new List<FSM_Transition>();

            foreach (var transition in Transitions)
                instant.Transitions.Add(transition.InstantiateTransition());

            return instant;
        }

        public void OnStateEnter()
        {
            foreach (var transition in Transitions)
            {
                transition.OnStateEnter();
            }
        }
        
        public void Update(float deltaTime)
        {
            foreach (var transition in Transitions)
            {
                if (!transition.Condition) continue;
                
                transition.Condition.UpdateCondition(deltaTime);
            }
        }

        public void SetUser(IFSMUser user)
        {
            foreach (var transition in Transitions)
            {
                transition.SetUser(user);
            }
        }

#if UNITY_EDITOR
        public void AddTransition(FSM_State toState)
        {
            foreach (var transition in Transitions)
            {
                if (transition.NextState == toState)
                {
                    Debug.LogWarning("This state already have the next state as an existing connection");
                    return;
                }
            }
            Transitions.Add(new FSM_Transition(toState));
        }


        public void RemoveTransition(FSM_State toState)
        {
            var transition = Transitions.Find(tran => tran.NextState == toState);

            if (transition != null)
            {
                if (transition.Condition)
                {
                    UnityEngine.Object.DestroyImmediate(transition.Condition, true);
                    AssetDatabase.Refresh();
                }

                Transitions.Remove(transition);
            }
            else
                Debug.LogWarning("No transition to be removed");
        }

#endif
    }
}