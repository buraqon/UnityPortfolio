using FiniteStateMachine;
using UnityEngine;

[System.Serializable]
public class FSM_State_GoToState : FSM_State
{
    [SerializeField] private FSM_State state;
    public override void StateEnter()
    {
    }

    public override void StateExit()
    {
    }

    protected override void OnInstantiate(FSM_State stateInstant)
    {
        stateInstant.transitions.Transitions.Add(new FSM_Transition(state).InstantiateTransition());
    }

    protected override void OnSetUser(IFSMUser user)
    {
    }

    protected override void OnStateUpdate(float deltaTime)
    {
    }
}
