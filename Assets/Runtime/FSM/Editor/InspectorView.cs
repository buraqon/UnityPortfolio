using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;
using FiniteStateMachine;
using HippoLib.Runtime.Util;

#if UNITY_EDITOR
[UxmlElement]
public partial class InspectorView : VisualElement
{
    Editor editor;

    public InspectorView()
    {
    }

    public void HideSelection()
    {
        style.display = DisplayStyle.None;
    }

    public void UpdateSelection(FSM_Transition transition, FSM machine)
    {
        Clear();
        UnityEngine.Object.DestroyImmediate(editor);
        style.display = DisplayStyle.Flex;

        Add(new Label($"Transition to: {(transition.NextState != null ? transition.NextState.name : "Null")}")
        {
            style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
        });

        if (transition.Condition != null)
        {
            editor = Editor.CreateEditor(transition.Condition);

            IMGUIContainer inspectorContainer = new IMGUIContainer(() =>
            {
                if (editor != null) editor.OnInspectorGUI();

                if (GUILayout.Button("Remove Condition"))
                {
                    machine.RemoveConditionFromTransition(transition);
                    UpdateSelection(transition, machine);
                }
            });
            Add(inspectorContainer);
        }
        else
        {
            Label title = new Label("Add Condition:");
            Add(title);

            var conditionTypes = TypeUtil.GetDerivedTypes(typeof(FSM_Condition));
            foreach (var type in conditionTypes)
            {
                Button btn = new Button(() => {
                    machine.AddConditionToTransition(transition, type);
                    UpdateSelection(transition, machine);
                })
                { text = type.Name };
                Add(btn);
            }
        }
    }
}

#endif