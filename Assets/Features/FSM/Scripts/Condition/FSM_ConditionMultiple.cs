using System;
using System.Collections.Generic;

namespace FiniteStateMachine
{
    public class FSM_ConditionMultiple : FSM_Condition
    {
        public List<FSM_Condition> conditions = new List<FSM_Condition>();
        public int indexToDelete = -1;
        public override void OnStateEnter()
        {
            foreach (var condition in conditions)
            {
                condition.OnStateEnter();
            }
        }

        public override bool EvaluateCondition()
        {
            foreach (var condition in conditions)
            {
                if (!condition.EvaluateCondition())
                    return false;
            }

            return true;
        }

        public override void UpdateCondition(float deltaTime)
        {
            foreach (var condition in conditions)
            {
                condition.UpdateCondition(deltaTime);
            }
        }

        protected override void OnInstantiate(FSM_Condition instant)
        {
            var multiple = (FSM_ConditionMultiple)instant;
            multiple.conditions = new List<FSM_Condition>();
            foreach (var condition in conditions)
            {
                multiple.conditions.Add(condition.InstantiateCondition());
            }
        }

        protected override void OnSetUser(IFSMUser user)
        {
            foreach (var condition in conditions)
            {
                condition.SetUser(user);
            }
        }
#if UNITY_EDITOR
        public override void OnConditionDeleted()
        {
            for (int i = conditions.Count - 1; i >= 0; i--)
            {
                var condition = conditions[i];
                FSM.DeleteCondition(condition);
            }
        }
#endif
    }
}