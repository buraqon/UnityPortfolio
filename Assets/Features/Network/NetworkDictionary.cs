using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkDictionary<TKey, TValue>
{
    public Action<NetworkListEvent<TKey>> OnKeyChanged;
    public Action<NetworkListEvent<TValue>> OnValueChanged;

    public NetworkListClass<TKey> Keys;
    public NetworkListClass<TValue> Values;

    public NetworkDictionary(IEnumerable<TKey> keys = default, IEnumerable<TValue> values = default 
        , NetworkVariableReadPermission readPerm = NetworkVariableReadPermission.Everyone
        , NetworkVariableWritePermission writePerm = NetworkVariableWritePermission.Server)
    {
        Keys = new NetworkListClass<TKey>(keys, readPerm, writePerm);
        Values = new NetworkListClass<TValue>(values, readPerm, writePerm);
    }

    public void Initialize(NetworkBehaviour networkBehaviour)
    {
        Keys.Initialize(networkBehaviour);
        Values.Initialize(networkBehaviour);
        Keys.OnListChanged += (even) => { OnKeyChanged?.Invoke(even); } ;
        Values.OnListChanged += (even) => { OnValueChanged?.Invoke(even); }; ;
    }

    public TValue this[TKey key]
    {
        get
        {
            return Values[Keys.IndexOf(key)];
        }
        set
        {
            Values[Keys.IndexOf(key)] = value;
        }
    }

    public void Add(TKey key, TValue value)
    {
        if (Keys.Contains(key))
            Debug.LogError($"{key} already exists in the dictionary");

        Keys.Add(key);
        Values.Add(value);
    }

    public void Remove(TKey key)
    {
        if (!Keys.Contains(key))
            Debug.LogError($"{key} isn't in the dictionary");

        Values.RemoveAt(Keys.IndexOf(key));
        Keys.Remove(key);
    }

    public bool ContainsKey(TKey key)
    {
        return Keys.Contains(key);
    }

    public Dictionary<TKey, TValue> GetDict()
    {
        var dict = new Dictionary<TKey, TValue>();
        for (int i = 0; i < Keys.Count; i++)
        {
            dict.Add(Keys[i], Values[i]);
        }
        return dict;
    }

    public int Count()
    {
        return Keys.Count;
    }
}