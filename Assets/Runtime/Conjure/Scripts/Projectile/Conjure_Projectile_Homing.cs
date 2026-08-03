using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HippoLib.Conjures
{
    public class Conjure_Projectile_Homing : Conjure_Projectile
    {
        private IConjureReciever _target;
        private Vector3 _velocity;

        public void SetTarget(IConjureReciever target)
        {
            _target = target;
        }
        
        protected override void MoveProjectile()
        {
            if (_target == null || !_target.IsAlive())
            {
                EndLife();
                return;
            }
            var targetDir = (_target.transform.position - transform.position).normalized;
            targetDir.y = 0;
            transform.forward = Vector3.SmoothDamp(transform.forward, targetDir, ref _velocity, 0.04f);
            base.MoveProjectile();
        }
                 
        protected override bool IsDone()
        {
            return false;
        }
    }
}