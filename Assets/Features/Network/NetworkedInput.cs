using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkedInput : NetworkBehaviour
{
    NetworkList<FixedString64Bytes> inputNames = new NetworkList<FixedString64Bytes>(new List<FixedString64Bytes>(),
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    NetworkList<int> indexes = new NetworkList<int>(new List<int>(), NetworkVariableReadPermission.Owner,
        NetworkVariableWritePermission.Owner);


    List<int> intList = new();

    List<double> doubleList = new();

    List<Vector2> vectorList = new();

    List<bool> boolList = new();

    public void Initialize(List<InputAction> networkedActions)
    {
        if (networkedActions == null)
        {
            Debug.LogError("NetworkedActions list is null.");
            return;
        }

        if (!NetworkObject.IsOwner && !IsServer) return;

        foreach (var action in networkedActions)
        {
            AddInputControl(action);
        }
    }

    private void AddInputControl(InputAction action)
    {
        var controlType = GetControlType(action);
        switch (controlType)
        {
            case ControlType.Integer:
                RegisterInt(action);
                break;
            
            case ControlType.Double:
                RegisterDouble(action);
                break;
            
            case ControlType.Vector2:
                RegisterVector(action);
                break;
            
            case ControlType.Button:
                RegisterBool(action);
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
            case null: 
                if (action.type == InputActionType.Button) 
                    return ControlType.Button; 
                else 
                    return ControlType.Unsupported;
            default: return ControlType.Unsupported;
        }
    }

    private void RegisterInt(InputAction action)
    {
        var index = intList.Count;
        intList.Add(0);

        if (!NetworkObject.IsOwner) return;

        inputNames.Add(action.name);
        indexes.Add(index);

        action.performed += (InputAction.CallbackContext context) =>
        {
            intList[index] = context.ReadValue<int>();
            IntListChangedServerRPC(index, intList[index]);
        };
        action.canceled += (InputAction.CallbackContext context) => 
        {
            intList[index] = 0;
            IntListChangedServerRPC(index, intList[index]);
        };
    }
    private void RegisterDouble(InputAction action)
    {
        var index = doubleList.Count;
        doubleList.Add(0);

        if (!NetworkObject.IsOwner) return;

        inputNames.Add(action.name);
        indexes.Add(index);

        action.performed += (InputAction.CallbackContext context) =>
        {
            doubleList[index] = context.ReadValue<double>();
            DoubleListChangedServerRPC(index, doubleList[index]);
        };
        action.canceled += (InputAction.CallbackContext context) =>
        {
            doubleList[index] = 0;
            DoubleListChangedServerRPC(index, doubleList[index]);
        };
    }
    private void RegisterVector(InputAction action)
    {
        var index = vectorList.Count;
        vectorList.Add(default);

        if (!NetworkObject.IsOwner) return;

        inputNames.Add(action.name);
        indexes.Add(index);

        action.performed += (InputAction.CallbackContext context) =>
        {
            vectorList[index] = context.ReadValue<Vector2>();
            VectorListChangedServerRPC(index, vectorList[index]);
        };
        action.canceled += (InputAction.CallbackContext context) =>
        {
            vectorList[index] = default;
            VectorListChangedServerRPC(index, vectorList[index]);
        };
    }
    private void RegisterBool(InputAction action)
    {
        var index = boolList.Count;
        boolList.Add(default);

        if (!NetworkObject.IsOwner) return;

        inputNames.Add(action.name);
        indexes.Add(index);

        action.started += _ => 
        { 
            boolList[index] = true; 
            BoolListChangedServerRPC(index, boolList[index]);
        };
        action.canceled += _ => 
        { 
            boolList[index] = false;
            BoolListChangedServerRPC(index, boolList[index]);
        };
    }

    [ServerRpc]
    private void IntListChangedServerRPC(int index, int value)
    {
        intList[index] = value;
    }
    [ServerRpc]
    private void DoubleListChangedServerRPC(int index, double value)
    {
        doubleList[index] = value;
    }
    [ServerRpc]
    private void VectorListChangedServerRPC(int index, Vector2 value)
    {
        vectorList[index] = value;
    }
    [ServerRpc]
    private void BoolListChangedServerRPC(int index, bool value)
    {
        boolList[index] = value;
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
    
    private void OnApplicationQuit()
    {
        Debug.Log("Application quit");

        inputNames.Dispose();
        indexes.Dispose();
        intList.Clear();
        doubleList.Clear();
        vectorList.Clear();
        boolList.Clear();
    }
}

public enum ControlType
{
    Vector2,
    Vector3,
    Integer,
    Double,
    Button,
    Unsupported
}