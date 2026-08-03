using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Linq;
using FiniteStateMachine;
using HippoLib;
using HippoLib.Runtime.Util;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Experimental.GraphView;


[UxmlElement]
public partial class FSM_View : GraphView
{
    private FSM machine;

    public FSM Machine { get => machine; }

    public InspectorView inspectorWindow;


    public FSM_View()
    {
        Insert(0, new GridBackground());

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var styleSheet = AssetDatabase_Utility.FindAndGetAsset<StyleSheet>("StyleSheet","FSM_Window");
        styleSheets.Add(styleSheet);

        this.RegisterCallback<PointerDownEvent>(evt => {
            if (evt.target == this && evt.button == 0)
            {
                inspectorWindow.HideSelection();
            }
        });
    }

    FSM_StateView FindStateView(FSM_State state)
    {
        return GetNodeByGuid(state.guid) as FSM_StateView;
    }

    public void PopulateView(FSM machine)
    {
        this.machine = machine;

        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements);
        graphViewChanged += OnGraphViewChanged;

        machine.States.ForEach(s => CreateStateView(s));
        FindStateView(machine.States[0]).SetAsInitialState();


        machine.States.ForEach(s => CreateTransitionView(s));
        if(machine.currentState != null)
        {
            var currentStateView = FindStateView(machine.currentState);
            if (currentStateView != null)
                currentStateView.SetAsCurrentState();
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(endport =>
        endport.direction != startPort.direction &&
        endport.node != startPort.node &&
        CheckIfEdgeAlreadyExist(startPort, endport)).ToList();
    }

    private bool CheckIfEdgeAlreadyExist(Port startPort, Port endport)
    {
        foreach (var edge in edges)
        {
            var outputPort = edge.output;
            var inputPort = edge.input;

            if (outputPort == startPort && inputPort == endport)
                return false;
        }

        return true;
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        if (graphViewChange.elementsToRemove != null)
        {
            foreach (var element in graphViewChange.elementsToRemove)
            {
                var stateView = element as FSM_StateView;
                if (stateView != null)
                    machine.DeletState(stateView.state);

                var edge = element as Edge;
                if (edge != null)
                {
                    FSM_StateView fromState = edge.output.node as FSM_StateView;
                    FSM_StateView toState = edge.input.node as FSM_StateView;
                    machine.RemoveTransitionFromTo(fromState.state, toState.state);
                }
            }
        }

        if (graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                edge.AddManipulator(new Clickable(ev => OnEdgeSeleceted(edge)));
                FSM_StateView fromState = edge.output.node as FSM_StateView;
                FSM_StateView toState = edge.input.node as FSM_StateView;
                machine.AddTransitionFromTo(fromState.state, toState.state);
            }
        }

        return graphViewChange;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        var types = TypeCache.GetTypesDerivedFrom<FSM_State>();
        foreach (var type in types)
        {
            if (!type.IsAbstract)
                evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", (a) => CreateState(type));
        }
    }

    private void CreateState(Type type)
    {
        FSM_State state = machine.CreateState(type);
        CreateStateView(state);
    }

    private void CreateStateView(FSM_State s)
    {
        FSM_StateView stateView = new FSM_StateView(s);
        AddElement(stateView);
    }
    private void CreateTransitionView(FSM_State fromState)
    {
        foreach (var transition in fromState.transitions.Transitions)
        {
            var toState = transition.NextState;
            var fromView = FindStateView(fromState);
            var toView = FindStateView(toState);

            Edge edge = fromView.output.ConnectTo(toView.input);
            edge.AddManipulator(new Clickable(ev => OnEdgeSeleceted(edge)));
            AddElement(edge);
        }
    }

    private void OnEdgeSeleceted(Edge edge)
    {
        FSM_StateView fromState = edge.output.node as FSM_StateView;
        FSM_StateView toState = edge.input.node as FSM_StateView;

        var trans = fromState.state.transitions.Transitions.Find(trans => trans.NextState == toState.state);

        inspectorWindow.UpdateSelection(trans, machine);
    }

}
#endif