using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class NetworkRefList<TValue> : NetworkBehaviour
{
    private NetworkList<int> idList = new();
    private List<TValue> valueList = new();

    public Action<TValue> OnValueAdded;
    public Action<TValue> OnValueRemoved;

    public void InitializeNetworkList()
    {
        idList.OnListChanged += OnIDChanged;

        if (!IsServer)
            ClientInitialSync();
    }

    private void ClientInitialSync()
    {
        for (int i = 0; i < idList.Count; i++)
            AddValue(idList[i]);
    }

    #region Getters

    public TValue GetAtIndex(int index) => valueList[index];
    public List<TValue> GetValues() => valueList;
    protected abstract int GetID(TValue value);
    protected abstract TValue GetValue(int id);
    public int GetCount() => valueList.Count;
    public TValue this[int i] => GetAtIndex(i);

    #endregion

    #region Setters

    public void Add(TValue value)
    {
        var id = GetID(value);
        idList.Add(id);

        if (IsServer)
            AddValue(value);
    }

    public void Remove(TValue value)
    {
        var id = GetID(value);
        idList.Remove(id);
        
        if (IsServer)
            RemoveValue(id);
    }

    public void RemoveAt(int index)
    {
        idList.RemoveAt(index);
        
        if (IsServer)
            RemoveValueAt(index);
    }

    #endregion

    #region LookUp Logic

    private void AddValue(int id)
    {
        var value = GetValue(id);
        AddValue(value);
    }

    private void AddValue(TValue value)
    {
        valueList.Add(value);
        OnValueAdded?.Invoke(value);
    }


    private void RemoveValue(int id)
    {
        var index = idList.IndexOf(id);
        var value = valueList[index];
        valueList.RemoveAt(index);
        OnValueRemoved?.Invoke(value);
    }

    private void RemoveValueAt(int index)
    {
        var value = valueList[index];
        valueList.RemoveAt(index);
        OnValueRemoved?.Invoke(value);
    }

    #endregion

    private void OnIDChanged(NetworkListEvent<int> changeEvent)
    {
        if(IsServer)
            return;
        
        if (changeEvent.Type == NetworkListEvent<int>.EventType.Add)
            AddValue(changeEvent.Value);
        else if (changeEvent.Type == NetworkListEvent<int>.EventType.Remove)
            RemoveValue(changeEvent.Value);
        else if (changeEvent.Type == NetworkListEvent<int>.EventType.RemoveAt)
            RemoveValueAt(changeEvent.Index);
    }
}