using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HippoLib.Effects
{
    public interface IEffectSender
    {
        public Transform transform { get; }
        public NetworkObject NetworkObject { get; }
    }
}