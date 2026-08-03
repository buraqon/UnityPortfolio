using System.Collections;
using System.Collections.Generic;
using HippoLib.Effects;
using HippoLib.Effects.Effector;
using UnityEngine;

public class Effector_Overtime<TSender, TReciever> : Effect_Effector<TSender, TReciever>
    where TSender : IEffectSender where TReciever : IEffectReciever
{
    private float triggerTime;
    private int triggerCount;
    
    private float timer;
    private int counter;
    
    public Effector_Overtime(float time, int count)
    {
        triggerTime = time;
        triggerCount = count;
    }
    
    protected override void OnAdded(TReciever reciever)
    {
        timer = triggerTime;
        counter = 0;
    }
    
    protected override void OnUpdate(TReciever reciever, float deltaTime)
    {
        timer += deltaTime;
        if (timer >= triggerTime)
        {
            timer = 0;
            counter++;
            TriggerEffect(reciever);
            if (counter >= triggerCount)
            {
                Finished();
            }
        }
    }
}