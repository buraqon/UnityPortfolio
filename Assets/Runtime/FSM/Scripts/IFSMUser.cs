using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiniteStateMachine
{
    public interface IFSMUser
    {
        public Transform transform { get; }
        float DeltaTime { get; }

#if UNITY_EDITOR
        public FSM CurrentFSM { get; }
#endif
    }
}