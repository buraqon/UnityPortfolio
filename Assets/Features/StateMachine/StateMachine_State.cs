using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.StateMachine
{
    public abstract class StateMachine_State
    {
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Update() { }
    }

}
