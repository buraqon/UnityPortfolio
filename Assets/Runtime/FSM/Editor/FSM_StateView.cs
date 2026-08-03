using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UIElements;
using FiniteStateMachine;
using HippoLib;

#if UNITY_EDITOR

using UnityEditor.Experimental.GraphView;
using UnityEditor;

public class FSM_StateView : Node
{
    public Action<FSM_StateView> OnStateSelected;
    public FSM_State state;
    public Port input;
    public Port output;

    Editor editor;
    TextField textField;
    Label titleField;

    bool renaming;

    public FSM_StateView(FSM_State state) : base(
        AssetDatabase_Utility.FindAndGetAssetPath<VisualTreeAsset>("VisualTreeAsset", "FSM_StateView"))
    {
        this.state = state;
        this.title = state.name;
        this.viewDataKey = state.guid;

        style.left = state.graphPosition.x;
        style.top = state.graphPosition.y;

        CreatingInputPorts();
        CreatingOutputPorts();
        CreatingFoldout();
        GettingTitleFields();
    }

    private void CreatingInputPorts()
    {
        input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        if (input != null)
        {
            input.portName = "In";
            input.portColor = Color.green;
            input.style.flexDirection = FlexDirection.Row;
            inputContainer.Add(input);
        }
    }

    private void CreatingOutputPorts()
    {
        output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
        if (output != null)
        {
            output.portName = "Out";
            output.portColor = Color.red;
            output.style.flexDirection = FlexDirection.RowReverse;
            outputContainer.Add(output);
        }
    }

    private void CreatingFoldout()
    {
        var foldout = mainContainer.Q<Foldout>("Foldout");
        var content = mainContainer.Q<VisualElement>("Values");

        foldout.RegisterValueChangedCallback(evt =>
        {
            content.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });

        content.style.display = foldout.value ? DisplayStyle.Flex : DisplayStyle.None;

        editor = Editor.CreateEditor(state);
        IMGUIContainer container = new IMGUIContainer(() => { editor.OnInspectorGUI(); });
        content.Add(container);
    }

    private void GettingTitleFields()
    {
        textField = this.Q<TextField>();
        titleField = this.Q<Label>();

        titleField.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button == 0 && evt.clickCount == 2)
            {
                Rename();
            }
        });
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        state.graphPosition.x = newPos.xMin;
        state.graphPosition.y = newPos.yMin;
        EditorUtility.SetDirty(state);
    }

    public override void OnSelected()
    {
        base.OnSelected();
        OnStateSelected?.Invoke(this);
    }

    public override void OnUnselected()
    {
        base.OnUnselected();
        if(renaming)
            EndRename();
    }

    private void EndRename()
    {
        if (textField.value != "")
        {
            state.name = textField.value;
            titleField.text = textField.value;
        }
        textField.style.display = DisplayStyle.None;
        titleField.style.display = DisplayStyle.Flex;

        renaming = false;
    }

    private void Rename()
    {
        textField.value = titleField.text;

        titleField.style.display = DisplayStyle.None;
        textField.style.display = DisplayStyle.Flex;

        renaming = true;
    }

    internal void SetAsCurrentState()
    {
        style.backgroundColor = Color.blue;
    }
    internal void SetAsInitialState()
    {
        style.borderTopColor = Color.green;
        style.borderTopWidth = 4;
    }
}
#endif