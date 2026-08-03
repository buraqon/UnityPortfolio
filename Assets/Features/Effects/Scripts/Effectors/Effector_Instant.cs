using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.Effects.Effector
{
    public class Effector_Instant<TSender, TReciever> : Effect_Effector<TSender, TReciever>
        where TSender : IEffectSender where TReciever : IEffectReciever
    {
        protected override void OnAdded(TReciever reciever)
        {
            TriggerEffect(reciever);
            Finished();
        }
    }
}