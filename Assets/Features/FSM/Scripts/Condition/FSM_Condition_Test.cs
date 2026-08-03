using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiniteStateMachine
{
    public class FSM_Condition_Test : FSM_Condition
    {
        private float time;
        public override bool EvaluateCondition()
        {
            Debug.Log("Evaulating condition, with current time " + time);
            return true;
        }

        public override void UpdateCondition(float deltaTime)
        {
            time += deltaTime;
        }

        public override void OnStateEnter()
        {
            time = 0;
            Debug.Log("On State enter");
        }
        protected override void OnInstantiate(FSM_Condition instant)
        {
            Debug.Log("On Instantiate");
        }

        protected override void OnSetUser(IFSMUser user)
        {
            Debug.Log("Setting User " + user);
        }
    }
}