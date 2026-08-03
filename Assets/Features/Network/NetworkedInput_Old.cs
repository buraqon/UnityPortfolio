using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkedInput_Old : NetworkBehaviour
{
    NetworkList<FixedString64Bytes> inputNames = new NetworkList<FixedString64Bytes>(new List<FixedString64Bytes>(),
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
    

    NetworkList<int> indexes = new NetworkList<int>(new List<int>(), NetworkVariableReadPermission.Owner,
        NetworkVariableWritePermission.Owner);

    NetworkList<int> intList = new NetworkList<int>(new List<int>(), NetworkVariableReadPermission.Owner,
        NetworkVariableWritePermission.Owner);

    NetworkList<double> doubleList = new NetworkList<double>(new List<double>(), NetworkVariableReadPermission.Owner,
        NetworkVariableWritePermission.Owner);

    NetworkList<Vector2> vectorList = new NetworkList<Vector2>(new List<Vector2>(), NetworkVariableReadPermission.Owner,
        NetworkVariableWritePermission.Owner);

    NetworkList<bool> boolList = new NetworkList<bool>(new List<bool>(), NetworkVariableReadPermission.Owner,
        NetworkVariableWritePermission.Owner);

    List<bool> boolPressedList = new List<bool>();

    Action resetBools;

    public void Initialize(List<InputAction> networkedActions)
    {
        if (networkedActions == null)
        {
            Debug.LogError("NetworkedActions list is null.");
            return;
        }

        boolList.OnListChanged += OnBoolListChanged;

        if (!NetworkObject.IsOwner) return;

        foreach (var action in networkedActions)
        {
            AddInputControl(action);
        }
    }

    private void OnBoolListChanged(NetworkListEvent<bool> changeEvent)
    {
        if(changeEvent.Type == NetworkListEvent<bool>.EventType.Add)
        {
            boolPressedList.Add(new bool());
            resetBools += () => { boolPressedList[changeEvent.Index] = false; };
        }
        if (changeEvent.Value)
            boolPressedList[changeEvent.Index] = true;
    }

    private void LateUpdate()
    {
        resetBools?.Invoke();
    }

    private void AddInputControl(InputAction action)
    {
        var controlType = GetControlType(action);
        int ind;
        switch (controlType)
        {
            case ControlType.Integer:
                RegisterInput(intList, action);
                break;
            
            case ControlType.Double:
                RegisterInput(doubleList, action);
                break;
            
            case ControlType.Vector2:
                RegisterInput(vectorList, action);
                break;
            
            case ControlType.Button:
                ind = boolList.Count;

                inputNames.Add(action.name);
                indexes.Add(ind);

                boolList.Add(new bool());
                action.started += _ => { boolList[ind] = true; };
                action.canceled += _ => { boolList[ind] = false; };
                break;
            default:
                Debug.LogWarning("Unsupported control type for action: " + action.name);
                break;
        }
    }

    private ControlType GetControlType(InputAction action)
    {
        var expectedControlType = action.expectedControlType;
        switch (expectedControlType)
        {
            case "Vector2": return ControlType.Vector2;
            case "Integer": return ControlType.Integer;
            case "Double": return ControlType.Double;
            case "Button": return ControlType.Button;
            default: return ControlType.Unsupported;
        }
    }

    private void RegisterInput<T>(NetworkList<T> inputList, InputAction action) where T : unmanaged, System.IEquatable<T>
    {
        var index = inputList.Count;
        inputNames.Add(action.name);
        indexes.Add(index);

        T def = default;
        inputList.Add(def);
        action.performed += (InputAction.CallbackContext context) =>
        {
            inputList[index] = context.ReadValue<T>();
        };
        action.canceled += (InputAction.CallbackContext context) => { inputList[index] = default; };
    }

    protected T Get<T>(InputAction action)
    {
        if (!NetworkObject.IsOwner && !IsServer)
        {
            Debug.LogWarning("Only owner or server can get input values.");
            return default;
        }

        T t;
        try
        {
            t = (T)Get(action);
        }
        catch (Exception e)
        {
            t = default;
            Debug.LogWarning(e);
        }

        return t;
    }

    private object Get(InputAction action)
    {
        var controlType = GetControlType(action);
        int ind = indexes[inputNames.IndexOf(action.name)];
        switch (controlType)
        {
            case ControlType.Integer:
                return intList[ind];
            case ControlType.Double:
                return doubleList[ind];
            case ControlType.Vector2:
                return vectorList[ind];
            case ControlType.Button:
                return boolList[ind];
            default:
                Debug.LogWarning("Unsupported control type for action: " + action.name);
                return null;
        }
    }

    protected bool GetBoolPressed(InputAction action)
    {
        if (!NetworkObject.IsOwner && !IsServer)
        {
            Debug.LogWarning("Only owner or server can get input values.");
            return default;
        }

        try
        {
            int ind = indexes[inputNames.IndexOf(action.name)];
            return boolPressedList[ind];
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            return false;
        }
    }
    
    
    private void OnApplicationQuit()
    {
        Debug.Log("Application quit");

        inputNames.Dispose();
        indexes.Dispose();
        intList.Dispose();
        doubleList.Dispose();
        vectorList.Dispose();
        boolList.Dispose();
    }
}