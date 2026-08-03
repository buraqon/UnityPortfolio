// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class Effector_Stack : Effects_Effector
// {
//     private float _time;

//     private float _timer;
//     private int _counter;
//     private ConsumeCases _consumeCase;

//     public Effector_Stack(float time, int count, ConsumeCases consumeCases)
//     {
//         _time = time;
//         _counter = count;
//         _consumeCase = consumeCases;
//     }

//     protected override void OnAdded()
//     {
//         _timer = 0;
//     }

//     protected override void OnUpdate(float deltaTime)
//     {
//         _timer += deltaTime;
//         if (_timer > _time)
//         {
//             Finished();
//         }
//     }

//     public override void OnDamaged(IConjureCaster damageSource, Unit_Main target)
//     {
//         Debug.Log(_counter);
//         base.OnDamaged(damageSource, target);
//         if (_consumeCase == ConsumeCases.Damage)
//             ConsumeStack();
//     }

//     public void ConsumeStack()
//     {
//         _counter--;
//         if (_counter < 1)
//         {
//             Finished();
//         }
//     }
// }


// public enum ConsumeCases
// {
//     Damage,
//     Heal,
//     TIme
// }