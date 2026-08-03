using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.StateMachine
{
    [System.Serializable]
    public abstract class StateMachine<T> where T : StateMachine_State
    {
        protected T mCurrentState;
        protected string stateName;
        public T GetCurrentState()
        {
            return mCurrentState;
        }

        public bool IsEqualState(T stateToCheck)
        {
            return mCurrentState == stateToCheck;
        }

        protected void SetState(T state)
        {
            // if (mCurrentState == state) return;

            if (mCurrentState != null)
                mCurrentState.Exit();

            mCurrentState = state;
            mCurrentState.Enter();
            
            stateName = mCurrentState.GetType().Name;
        }

        public void Update()
        {
            if (mCurrentState != null)
                mCurrentState.Update();

            OnUpdate();
        }

        protected virtual void OnUpdate() { }
    }
}

