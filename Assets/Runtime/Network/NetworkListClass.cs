using System;
using System.Collections.Generic;
using Unity.Netcode;

[GenerateSerializationForGenericParameter(0)]
public class NetworkListClass<T> : NetworkVariableBase

{
    private List<T> m_List = new List<T>();
    private List<NetworkListEvent<T>> m_DirtyEvents = new List<NetworkListEvent<T>>();

    /// <summary>
    /// Delegate type for list changed event
    /// </summary>
    /// <param name="changeEvent">Struct containing information about the change event</param>
    public delegate void OnListChangedDelegate(NetworkListEvent<T> changeEvent);

    /// <summary>
    /// The callback to be invoked when the list gets changed
    /// </summary>
    public event OnListChangedDelegate OnListChanged;

    /// <summary>
    /// Constructor method for <see cref="NetworkList"/>
    /// </summary>
    public NetworkListClass()
    {
    }

    /// <inheritdoc/>
    /// <param name="values"></param>
    /// <param name="readPerm"></param>
    /// <param name="writePerm"></param>
    public NetworkListClass(IEnumerable<T> values = default,
        NetworkVariableReadPermission readPerm = DefaultReadPerm,
        NetworkVariableWritePermission writePerm = DefaultWritePerm)
        : base(readPerm, writePerm)
    {
        // allow null IEnumerable<T> to mean "no values"
        if (values != null)
        {
            foreach (var value in values)
            {
                m_List.Add(value);
            }
        }
    }

    /// <inheritdoc />
    public override void ResetDirty()
    {
        base.ResetDirty();
        if (m_DirtyEvents.Count > 0)
        {
            m_DirtyEvents.Clear();
        }
    }

    public override bool IsDirty()
    {
        return base.IsDirty() || m_DirtyEvents.Count > 0;
    }

    internal void MarkNetworkObjectDirty()
    {
        MarkNetworkBehaviourDirty();
    }

    public override void WriteDelta(FastBufferWriter writer)
    {
        if (base.IsDirty())
        {
            writer.WriteValueSafe((ushort)1);
            writer.WriteValueSafe(NetworkListEvent<T>.EventType.Full);
            WriteField(writer);

            return;
        }

        writer.WriteValueSafe((ushort)m_DirtyEvents.Count);
        for (int i = 0; i < m_DirtyEvents.Count; i++)
        {
            var element = m_DirtyEvents[i];
            writer.WriteValueSafe(element.Type);
            switch (element.Type)
            {
                case NetworkListEvent<T>.EventType.Add:
                {
                    NetworkVariableSerialization<T>.Write(writer, ref element.Value);
                }
                    break;
                case NetworkListEvent<T>.EventType.Insert:
                {
                    writer.WriteValueSafe(element.Index);
                    NetworkVariableSerialization<T>.Write(writer, ref element.Value);
                }
                    break;
                case NetworkListEvent<T>.EventType.Remove:
                {
                    NetworkVariableSerialization<T>.Write(writer, ref element.Value);
                }
                    break;
                case NetworkListEvent<T>.EventType.RemoveAt:
                {
                    writer.WriteValueSafe(element.Index);
                }
                    break;
                case NetworkListEvent<T>.EventType.Value:
                {
                    writer.WriteValueSafe(element.Index);
                    NetworkVariableSerialization<T>.Write(writer, ref element.Value);
                }
                    break;
                case NetworkListEvent<T>.EventType.Clear:
                {
                    //Nothing has to be written
                }
                    break;
            }
        }
    }

    /// <inheritdoc />
    public override void WriteField(FastBufferWriter writer)
    {
        writer.WriteValueSafe((ushort)m_List.Count);
        for (int i = 0; i < m_List.Count; i++)
        {
            T element = m_List[i];
            NetworkVariableSerialization<T>.Write(writer, ref element);
        }
    }

    /// <inheritdoc />
    public override void ReadField(FastBufferReader reader)
    {
        m_List.Clear();
        reader.ReadValueSafe(out ushort count);
        for (int i = 0; i < count; i++)
        {
            T value = default;
            NetworkVariableSerialization<T>.Read(reader, ref value);
            m_List.Add(value);
        }
    }

    /// <inheritdoc />
    public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
    {
        reader.ReadValueSafe(out ushort deltaCount);
        for (int i = 0; i < deltaCount; i++)
        {
            reader.ReadValueSafe(out NetworkListEvent<T>.EventType eventType);
            switch (eventType)
            {
                case NetworkListEvent<T>.EventType.Add:
                {
                    T value = default;
                    NetworkVariableSerialization<T>.Read(reader, ref value);
                    m_List.Add(value);

                    if (OnListChanged != null)
                    {
                        OnListChanged(new NetworkListEvent<T>
                        {
                            Type = eventType,
                            Index = m_List.Count - 1,
                            Value = m_List[m_List.Count - 1]
                        });
                    }

                    if (keepDirtyDelta)
                    {
                        m_DirtyEvents.Add(new NetworkListEvent<T>()
                        {
                            Type = eventType,
                            Index = m_List.Count - 1,
                            Value = m_List[m_List.Count - 1]
                        });
                        MarkNetworkObjectDirty();
                    }
                }
                    break;
                case NetworkListEvent<T>.EventType.Insert:
                {
                    reader.ReadValueSafe(out int index);
                    T value = default;
                    NetworkVariableSerialization<T>.Read(reader, ref value);

                    if (index < m_List.Count)
                    {
                        List<T> subList = new List<T> { value };
                        m_List.InsertRange(index, subList);
                    }
                    else
                    {
                        m_List.Add(value);
                    }

                    if (OnListChanged != null)
                    {
                        OnListChanged(new NetworkListEvent<T>
                        {
                            Type = eventType,
                            Index = index,
                            Value = m_List[index]
                        });
                    }

                    if (keepDirtyDelta)
                    {
                        m_DirtyEvents.Add(new NetworkListEvent<T>()
                        {
                            Type = eventType,
                            Index = index,
                            Value = m_List[index]
                        });
                        MarkNetworkObjectDirty();
                    }
                }
                    break;
                case NetworkListEvent<T>.EventType.Remove:
                {
                    T value = default;
                    NetworkVariableSerialization<T>.Read(reader, ref value);
                    int index = m_List.IndexOf(value);
                    if (index == -1)
                    {
                        break;
                    }

                    m_List.RemoveAt(index);

                    if (OnListChanged != null)
                    {
                        OnListChanged(new NetworkListEvent<T>
                        {
                            Type = eventType,
                            Index = index,
                            Value = value
                        });
                    }

                    if (keepDirtyDelta)
                    {
                        m_DirtyEvents.Add(new NetworkListEvent<T>()
                        {
                            Type = eventType,
                            Index = index,
                            Value = value
                        });
                        MarkNetworkObjectDirty();
                    }
                }
                    break;
                case NetworkListEvent<T>.EventType.RemoveAt:
                {
                    reader.ReadValueSafe(out int index);
                    T value = m_List[index];
                    m_List.RemoveAt(index);

                    if (OnListChanged != null)
                    {
                        OnListChanged(new NetworkListEvent<T>
                        {
                            Type = eventType,
                            Index = index,
                            Value = value
                        });
                    }

                    if (keepDirtyDelta)
                    {
                        m_DirtyEvents.Add(new NetworkListEvent<T>()
                        {
                            Type = eventType,
                            Index = index,
                            Value = value
                        });
                        MarkNetworkObjectDirty();
                    }
                }
                    break;
                case NetworkListEvent<T>.EventType.Value:
                {
                    reader.ReadValueSafe(out int index);
                    T value = default;
                    NetworkVariableSerialization<T>.Read(reader, ref value);
                    if (index >= m_List.Count)
                    {
                        throw new Exception("Shouldn't be here, index is higher than list length");
                    }

                    var previousValue = m_List[index];
                    m_List[index] = value;

                    if (OnListChanged != null)
                    {
                        OnListChanged(new NetworkListEvent<T>
                        {
                            Type = eventType,
                            Index = index,
                            Value = value,
                            PreviousValue = previousValue
                        });
                    }

                    if (keepDirtyDelta)
                    {
                        m_DirtyEvents.Add(new NetworkListEvent<T>()
                        {
                            Type = eventType,
                            Index = index,
                            Value = value,
                            PreviousValue = previousValue
                        });
                        MarkNetworkObjectDirty();
                    }
                }
                    break;
                case NetworkListEvent<T>.EventType.Clear:
                {
                    //Read nothing
                    m_List.Clear();

                    if (OnListChanged != null)
                    {
                        OnListChanged(new NetworkListEvent<T>
                        {
                            Type = eventType,
                        });
                    }

                    if (keepDirtyDelta)
                    {
                        m_DirtyEvents.Add(new NetworkListEvent<T>()
                        {
                            Type = eventType
                        });
                        MarkNetworkObjectDirty();
                    }
                }
                    break;
                case NetworkListEvent<T>.EventType.Full:
                {
                    ReadField(reader);
                    ResetDirty();
                }
                    break;
            }
        }
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        return m_List.GetEnumerator();
    }

    /// <inheritdoc />
    public void Add(T item)
    {
        // check write permissions
        if (!CanClientWrite(GetBehaviour().NetworkManager.LocalClientId))
        {
            throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
        }

        m_List.Add(item);

        var listEvent = new NetworkListEvent<T>()
        {
            Type = NetworkListEvent<T>.EventType.Add,
            Value = item,
            Index = m_List.Count - 1
        };

        HandleAddListEvent(listEvent);
    }

    /// <inheritdoc />
    public void Clear()
    {
        // check write permissions
        if (!CanClientWrite(GetBehaviour().NetworkManager.LocalClientId))
        {
            throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
        }

        m_List.Clear();

        var listEvent = new NetworkListEvent<T>()
        {
            Type = NetworkListEvent<T>.EventType.Clear
        };

        HandleAddListEvent(listEvent);
    }

    /// <inheritdoc />
    public bool Contains(T item)
    {
        return m_List.Contains(item);
    }

    /// <inheritdoc />
    public bool Remove(T item)
    {
        // check write permissions
        if (!CanClientWrite(GetBehaviour().NetworkManager.LocalClientId))
        {
            throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
        }

        int index = m_List.IndexOf(item);
        if (index == -1)
        {
            return false;
        }

        m_List.RemoveAt(index);
        var listEvent = new NetworkListEvent<T>()
        {
            Type = NetworkListEvent<T>.EventType.Remove,
            Value = item
        };

        HandleAddListEvent(listEvent);
        return true;
    }

    /// <inheritdoc />
    public int Count => m_List.Count;

    /// <inheritdoc />
    public int IndexOf(T item)
    {
        return m_List.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(int index, T item)
    {
        // check write permissions
        if (!CanClientWrite(GetBehaviour().NetworkManager.LocalClientId))
        {
            throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
        }

        if (index < m_List.Count)
        {
            List<T> subList = new List<T> { item };
            m_List.InsertRange(index, subList);
        }
        else
        {
            m_List.Add(item);
        }

        var listEvent = new NetworkListEvent<T>()
        {
            Type = NetworkListEvent<T>.EventType.Insert,
            Index = index,
            Value = item
        };

        HandleAddListEvent(listEvent);
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        // check write permissions
        if (!CanClientWrite(GetBehaviour().NetworkManager.LocalClientId))
        {
            throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
        }

        var value = m_List[index];
        m_List.RemoveAt(index);

        var listEvent = new NetworkListEvent<T>()
        {
            Type = NetworkListEvent<T>.EventType.RemoveAt,
            Index = index,
            Value = value
        };

        HandleAddListEvent(listEvent);
    }

    /// <inheritdoc />
    public T this[int index]
    {
        get => m_List[index];
        set
        {
            // check write permissions
            if (!CanClientWrite(GetBehaviour().NetworkManager.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
            }

            var previousValue = m_List[index];
            m_List[index] = value;

            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.Value,
                Index = index,
                Value = value,
                PreviousValue = previousValue
            };

            HandleAddListEvent(listEvent);
        }
    }

    private void HandleAddListEvent(NetworkListEvent<T> listEvent)
    {
        m_DirtyEvents.Add(listEvent);
        MarkNetworkObjectDirty();
        OnListChanged?.Invoke(listEvent);
    }

    /// <summary>
    /// This is actually unused left-over from a previous interface
    /// </summary>
    public int LastModifiedTick
    {
        get
        {
            // todo: implement proper network tick for NetworkList
            return NetworkTickSystem.NoTick;
        }
    }

    /// <summary>
    /// Overridden <see cref="IDisposable"/> implementation.
    /// CAUTION: If you derive from this class and override the <see cref="Dispose"/> method,
    /// you **must** always invoke the base.Dispose() method!
    /// </summary>
    public override void Dispose()
    {
        m_List.Clear();
        m_DirtyEvents.Clear();

        m_List = new List<T>();
        m_DirtyEvents = new List<NetworkListEvent<T>>();
    }

    protected bool Equals(NetworkListClass<T> other)
    {
        return Equals(m_List, other.m_List) && Equals(m_DirtyEvents, other.m_DirtyEvents);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((NetworkListClass<T>)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(m_List, m_DirtyEvents);
    }
}