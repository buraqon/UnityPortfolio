using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FiniteStateMachine
{
    public class FSM_Condition_Wait : FSM_Condition
    {
        public float Time = 5;
        private float timer;

        public override void OnStateEnter()
        {
            timer = 0;
        }

        public override bool EvaluateCondition()
        {
            if (timer > Time) return true;

            return false;
        }

        public override void UpdateCondition(float deltaTime)
        {
            timer += deltaTime;
        }

        protected override void OnInstantiate(FSM_Condition instant) { }
        protected override void OnSetUser(IFSMUser user) { }
    }
}