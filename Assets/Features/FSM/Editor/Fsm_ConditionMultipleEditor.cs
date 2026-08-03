#if UNITY_EDITOR
using FiniteStateMachine;
using HippoLib.Runtime.Util;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(FSM_ConditionMultiple))]
public class Fsm_ConditionMultipleEditor : Editor
{
    private FSM_ConditionMultiple conditionMultiple;

    public override void OnInspectorGUI()
    {
        conditionMultiple = (FSM_ConditionMultiple)target;
        base.OnInspectorGUI();
        
        var conditionTypes = TypeUtil.GetDerivedTypes(typeof(FSM_Condition));
        foreach (var conditionType in conditionTypes)
        {
            if (GUILayout.Button(conditionType.Name))
            {
                var fsm = conditionMultiple.FSM;
                var condition = fsm.CreateCondition(conditionType);
                conditionMultiple.conditions.Add(condition);
                EditorUtility.SetDirty(conditionMultiple);
            }
        }
        
        GUILayout.Space(30);

        if (GUILayout.Button("Delete at Index"))
        {
            var conditionList = conditionMultiple.conditions;
            var indexToDelete = conditionMultiple.indexToDelete;
            if (indexToDelete < 0 || indexToDelete >= conditionList.Count)
                return;
            
            
            var condition = conditionMultiple.conditions[conditionMultiple.indexToDelete];
            var fsm = conditionMultiple.FSM;
            conditionMultiple.conditions.RemoveAt(indexToDelete);
            fsm.DeleteCondition(condition);
            EditorUtility.SetDirty(conditionMultiple);
        }
        GUILayout.Space(60);
    }
}
#endif