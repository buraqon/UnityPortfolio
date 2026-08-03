using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.Effects
{
    using Effector;
    
public class Effect_Instant<TSender, TReciever> : Effect_Data<TSender, TReciever> where TSender : IEffectSender where TReciever : IEffectReciever
{
    protected override Effect_Effector<TSender, TReciever> InstantiateEffector()
    {
        return new Effector_Instant<TSender, TReciever>();
    }
}

}