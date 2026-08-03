using System;
using UnityEngine;

namespace HippoLib.Movement
{
    public class MovementHandler_ForceMovment
    {
        protected MovementHandler moveHandler;
        private Action onDone;
        private bool isDone;
        public bool IsDone => isDone;

        public MovementHandler_ForceMovment(MovementHandler moevmentHandler, Action onDone)
        {
            moveHandler = moevmentHandler;
            this.onDone = onDone;
            isDone = false;
        }

        public void Update()
        {
            OnUpdate();
        }

        protected virtual void OnUpdate()
        {
        }

        public void FinishMovement()
        {
            onDone?.Invoke();
            isDone = true;
        }

        public virtual bool IsInterruptable()
        {
            return true;
        }
    }

    public class MovementHandler_ForcedMovementDemo : MovementHandler_ForceMovment
    {
        private readonly Vector3 dashVector;
        private readonly float speed;

        private float time;
        private float totalTime;


        public MovementHandler_ForcedMovementDemo(MovementHandler moevmentHandler, Vector3 dashVector, float speed,
            Action onDone)
            : base(moevmentHandler, onDone)
        {
            this.dashVector = dashVector;
            this.speed = speed;

            totalTime = dashVector.magnitude / speed;
            time = 0;
        }

        protected override void OnUpdate()
        {
            time += Time.deltaTime;

            // moveHandler.HandleMovement(dashVector.normalized * speed);
            // moveHandler.HandleRotation(dashVector.normalized);
            
            moveHandler.ForceMove(dashVector.normalized * speed, dashVector.normalized, time/totalTime);

            if (time >= totalTime)
                FinishMovement();
        }
    }
}