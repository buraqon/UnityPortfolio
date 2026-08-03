using System;
using UnityEngine;

namespace HippoLib
{
    public class CollisionRelay : MonoBehaviour
    {
        public Action<Collider> EnterTrigger { get; set; }
        // public Action<Collider> StayTrigger { get; set; }
        public Action<Collider> ExitTrigger { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            EnterTrigger?.Invoke(other);
        }

        // private void OnTriggerStay(Collider other)
        // {
        //     StayTrigger?.Invoke(other);
        // }

        private void OnTriggerExit(Collider other)
        {
            ExitTrigger?.Invoke(other);
        }
    }
}
