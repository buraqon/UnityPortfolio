using HippoLib.Effects;
using HippoLib.Effects.Effector;
using UnityEngine;

public class Effect_DataList<TSender, TReciever> : NetworkRefList<Effect_Effector<TSender, TReciever>>
where TSender : IEffectSender where TReciever : IEffectReciever
{
    protected override int GetID(Effect_Effector<TSender, TReciever> value)
    {
        return 0; // to be overriden
    }

    protected override Effect_Effector<TSender, TReciever> GetValue(int id)
    {
        return null; // to be overriden
    }
}
