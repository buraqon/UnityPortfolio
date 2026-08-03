using UnityEngine.UIElements;
using UnityEngine;
using System;
using FiniteStateMachine;
using HippoLib;

#if UNITY_EDITOR
using UnityEditor;
public class FSM_Window : EditorWindow
{
    private FSM_View machineView;


    [MenuItem("Game/FSM_Window")]
    public static void ShowExample()
    {
        FSM_Window wnd = GetWindow<FSM_Window>();
        wnd.titleContent = new GUIContent("FSM_Window");
    }

    public static bool OnOpenAsset(int instanceId, int line) {
        if(Selection.activeObject is FSM)
        {
            ShowExample();
            return true;
        }
        return false;
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

       var visualTree = AssetDatabase_Utility.FindAndGetAsset<VisualTreeAsset>("VisualTreeAsset","FSM_Window");
        visualTree.CloneTree(root);

        var styleSheet = AssetDatabase_Utility.FindAndGetAsset<StyleSheet>("StyleSheet","FSM_Window");
        root.styleSheets.Add(styleSheet);

        machineView = root.Q<FSM_View>();
        machineView.inspectorWindow = root.Q<InspectorView>();
        OnSelectionChange();
    }

    private void OnSelectionChange()
    {
        var machine = Selection.activeObject as FSM;
        if (machine && AssetDatabase.CanOpenAssetInEditor(machine.GetInstanceID()))
        {
            PopulateView(machine);
            return;
        }
        var gObject = Selection.activeObject as GameObject;
        if (gObject)
        {
            var fsmUser = gObject.GetComponent<IFSMUser>();
            if (fsmUser != null)
            {
                PopulateView(fsmUser.CurrentFSM);
            }
        }
    }

    private void PopulateView(FSM machine)
    {
        if(machineView.Machine != null)
            machineView.Machine.OnCurrentStateChanged = null;

        machineView.PopulateView(machine);
        machine.OnCurrentStateChanged = () => machineView.PopulateView(machine);
    }    
}
#endif