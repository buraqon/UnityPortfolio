using UnityEngine;

public interface IEntity
{
    GameObject gameObject { get; }
    Transform transform { get; }
}
