using System.Collections;
using HippoLib.Effects;
using HippoLib.Effects.Effector;

public class Effect_Overtime<TSender, TReciever> : Effect_Data<TSender, TReciever>
    where TSender : IEffectSender where TReciever : IEffectReciever
{
    public float TriggerTime = 0.2f;
    public int TriggerCout = 1;

    protected override Effect_Effector<TSender, TReciever> InstantiateEffector()
    {
        return new Effector_Overtime<TSender, TReciever>(TriggerTime, TriggerCout);
    }
}