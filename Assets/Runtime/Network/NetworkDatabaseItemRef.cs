using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public abstract class NetworkDatabaseItemRef<T> : INetworkSerializable
{
    protected int id;
    protected T itemValue;

    public int ID { get { return id; } set { id = value;  itemValue = OnIdChanged(id); } }
    public T ItemValue { get { return itemValue; } set { itemValue = value; id = OnValueChanged(itemValue); } }

    void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        if (serializer.IsReader)
        {
            int id = ID;
            serializer.GetFastBufferReader().ReadValueSafe(out id);
            ID = id;
        }
        if (serializer.IsWriter)
            serializer.GetFastBufferWriter().WriteValueSafe(ID);
    }

    public abstract T OnIdChanged(int id);
    public abstract int OnValueChanged(T value);

    // public bool Equals(NetworkDatabaseItemRef<T> other)
    // {
    //     return id == other.ID;
    // }
    // public override bool Equals(object obj)
    // {
    //     if (obj == null) return false;
    //     NetworkDatabaseItemRef<T> objAsPart = obj as NetworkDatabaseItemRef<T>;
    //     if (objAsPart == null) return false;
    //     else return Equals(objAsPart);
    // }
    //
    // public override int GetHashCode()
    // {
    //     return id;
    // }
}
