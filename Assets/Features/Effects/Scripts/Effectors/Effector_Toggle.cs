using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.Effects.Effector
{
    public class Effector_Toggle<TSender, TReciever> : Effect_Effector<TSender, TReciever>
        where TSender : IEffectSender where TReciever : IEffectReciever
    {
        private float time;
        private bool triggerOnStart;

        private float timer;

        public Effector_Toggle(float time, bool triggerOnStart)
        {
            this.time = time;
            this.triggerOnStart = triggerOnStart;
        }

        protected override void OnAdded(TReciever reciever)
        {
            timer = 0;
            if (triggerOnStart)
            {
                TriggerEffect(reciever);
            }
        }

        protected override void OnUpdate(TReciever reciever, float deltaTime)
        {
            timer += deltaTime;
            if (timer > time)
            {
                TriggerEffect(reciever);
                Finished();
            }
        }
    }
}