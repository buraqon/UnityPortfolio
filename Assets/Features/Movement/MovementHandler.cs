using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HippoLib.Movement
{
    public class MovementHandler : NetworkBehaviour
    {
        [SerializeField] protected float Speed = 5;

        public virtual bool CanMove { get; }
        public virtual bool CanRotate { get; }

        protected Vector3 movementDirection;

        protected Vector3 lookDirection;
        protected Vector3 rotationVelocity;
        protected float RotSpeed = 1080f;

        private MovementHandler_ForceMovment forcedMovement;

        private Vector3 v;

        public Vector3 Velocity
        {
            get { return v; }
            set
            {
                OnVelocityChanged?.Invoke(value);
                v = value;
            }
        }

        public Action<Vector3> OnVelocityChanged;
        private Vector3 cachedPosition;

        public override void OnNetworkSpawn()
        {
            movementDirection = Vector3.zero;
            lookDirection = transform.forward;

            OnSpawn();
            //NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
        }

        protected virtual void OnSpawn()
        {
        }

        public void Update()
        {
            Velocity = (transform.position - cachedPosition) / Time.deltaTime;
            cachedPosition = transform.position;

            if (!NetworkObject.IsOwner && !IsServer)
                return;

            if (forcedMovement != null)
            {
                UpdateForcedMovement();
                return;
            }

            if (CanMove)
                HandleMovement(movementDirection * Speed);
            else
                HandleMovement(Vector3.zero);

            if (CanRotate)
                HandleRotation(lookDirection);

            //Velocity = (transform.position - cachedPosition) / Time.deltaTime;
            //cachedPosition = transform.position;

            OnUpdate();
        }

        private void UpdateForcedMovement()
        {
            forcedMovement.Update();

            if (forcedMovement.IsDone)
            {
                OnForcedMovementEnd();
                forcedMovement = null;
            }
        }

        protected virtual void OnForcedMovementStart() {}
        public virtual void ForceMove(Vector3 dashVectorNormalized, Vector3 normalized, float progress) { }
        protected virtual void OnForcedMovementEnd() { }

        protected virtual void OnUpdate()
        {
        }

        public void MoveInDirection(Vector3 direction)
        {
            movementDirection = direction.normalized;
        }

        public void LookInDirection(Vector3 direction)
        {
            lookDirection = direction;
        }

        public void SetCurrentSpeed(float speed)
        {
            Speed = speed;
        }

        public virtual void HandleMovement(Vector3 velocity)
        {
            transform.position += velocity * Time.deltaTime;
        }

        public virtual void HandleRotation(Vector3 lookDir)
        {
            transform.forward =
                Vector3.SmoothDamp(transform.forward, lookDir, ref rotationVelocity, 1 / RotSpeed);
        }

        public void AddForceMovement(MovementHandler_ForceMovment forceMovment)
        {
            if (forcedMovement != null)
            {
                if (forcedMovement.IsInterruptable())
                    forcedMovement.FinishMovement();
                else
                    return;
            }

            forcedMovement = forceMovment;
            OnForcedMovementStart();
        }

        public void OnDoneDashing()
        {
        }
    }
}