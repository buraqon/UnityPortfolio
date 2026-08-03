using System;
using System.Collections;
using System.Collections.Generic;
using FiniteStateMachine;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "_FSM", menuName = "FSM/_Machine")]
public class FSM : ScriptableObject
{
    // private FSM_State initialState;

    public List<FSM_State> States = new List<FSM_State>();
#if UNITY_EDITOR
    private FSM_State _currentState;
    public FSM_State currentState
    {
        get => _currentState; 
        set
        {
            _currentState = value;
            OnCurrentStateChanged?.Invoke();
        }
    }
    public Action OnCurrentStateChanged;
#else
    private FSM_State currentState;
#endif


    public void StartMachine(IFSMUser use)
    {
        foreach (var state in States)
        {   
            state.SetUser(use);
        }
        TransitionToState(States[0]);
    }

    public void UpdateMachine(IFSMUser user)
    {
        if (currentState != null)
        {
            currentState.StateUpdate(user.DeltaTime);
            var nextState = currentState.GetNextState();
            if (nextState && nextState != currentState)
                TransitionToState(nextState);
        }
    }

    public void TransitionToState(FSM_State nextState)
    {
        if (currentState != null)
            currentState.StateExit();

        currentState = nextState;

        if (currentState != null)
        {
            currentState.transitions.OnStateEnter();
            currentState.StateEnter();
        }
    }

    public FSM InstantiateFSM()
    {
        var stateMachineInstant = Instantiate(this);
        OnInstantiateFSM(stateMachineInstant);
        return stateMachineInstant;
    }

    private void OnInstantiateFSM(FSM stateMachineInstant)
    {
        stateMachineInstant.States = new List<FSM_State>();
        foreach (var stateSO in States)
        {
            var statInstant = stateSO.InstantiateState();
            stateMachineInstant.States.Add(statInstant);
        }
        
        foreach (var stateInstant in stateMachineInstant.States)
        {
            foreach (var transitionInsant in stateInstant.transitions.Transitions)
            {
                foreach (var otherStateInstant in stateMachineInstant.States)
                {
                    if (transitionInsant.NextState.guid == otherStateInstant.guid)
                    {
                        transitionInsant.NextState = otherStateInstant;
                        break;
                    }
                }
            }
        }

        foreach (var stateInstant in stateMachineInstant.States)
        {
            stateInstant.AfterInstantiate();
        }
    }


#if UNITY_EDITOR
    public FSM_State CreateState(Type type)
    {
        FSM_State state = ScriptableObject.CreateInstance(type) as FSM_State;
        state.name = type.Name;
        state.guid = GUID.Generate().ToString();
        States.Add(state);

        AssetDatabase.AddObjectToAsset(state, this);
        AssetDatabase.SaveAssets();
        return state;
    }

    public void DeletState(FSM_State state)
    {
        States.Remove(state);
        AssetDatabase.RemoveObjectFromAsset(state);
        AssetDatabase.SaveAssets();
    }

    public void AddTransitionFromTo(FSM_State fromState, FSM_State toState)
    {
        fromState.AddTransitionTo(toState);
    }

    public void RemoveTransitionFromTo(FSM_State fromState, FSM_State toState)
    {
        fromState.RemoveTransitionTo(toState);
    }
    
    public void AddConditionToTransition(FSM_Transition transition, Type conditionType)
    {
        transition.Condition = CreateCondition(conditionType);
    }
    
    public void RemoveConditionFromTransition(FSM_Transition transition)
    {
        DeleteCondition(transition.Condition);
        transition.Condition.OnConditionDeleted();
        transition.Condition = null;
    }

    public FSM_Condition CreateCondition(Type conditionType)
    {
        var newCondition = CreateInstance(conditionType) as FSM_Condition;
        newCondition.name = conditionType.Name;
        newCondition.SetFSM(this);
        
        AssetDatabase.AddObjectToAsset(newCondition, this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return newCondition;
    }

    public void DeleteCondition(FSM_Condition condition)
    {
        AssetDatabase.RemoveObjectFromAsset(condition);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.SetDirty(this);
    }
    
#endif
}
