using System.Collections;
using System.Collections.Generic;
using HippoLib;
using Unity.Netcode;
using UnityEngine;

public interface IConjureSender
{
    Transform transform { get; }
    public NetworkObject NetworkObject { get; }
    public float DeltaTime { get; }

    Transform VisualTransform { get; }
    Vector3 MousePosition { get; }

    bool IsTarget(IConjureReciever reciever);
}