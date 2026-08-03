using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.Effects
{
    using Effector;

    public abstract class Effect_Toggle<TSender, TReciever> : Effect_Data<TSender, TReciever>
        where TSender : IEffectSender where TReciever : IEffectReciever
    {
        public float Time = 1f;
        public bool TriggerOnStart = true;

        protected override Effect_Effector<TSender, TReciever> InstantiateEffector()
        {
            return new Effector_Toggle<TSender, TReciever>(Time, TriggerOnStart);
        }
    }
}