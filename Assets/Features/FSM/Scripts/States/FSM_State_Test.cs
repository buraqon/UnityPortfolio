using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiniteStateMachine
{
    [System.Serializable]
    public class FSM_State_Test : FSM_State
    {
        public override void StateEnter()
        {
            Debug.Log("Enter");
        }

        protected override void OnStateUpdate(float deltaTime)
        {
            Debug.Log("Update");
        }
        public override void StateExit()
        {
            Debug.Log("Exit");
        }

        protected override void OnInstantiate(FSM_State stateInstant)
        {
        }

        protected override void OnSetUser(IFSMUser user) { }
    }
}