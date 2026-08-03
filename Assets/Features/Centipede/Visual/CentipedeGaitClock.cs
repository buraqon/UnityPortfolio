using UnityEngine;

namespace Centipede.Visual
{
    [ExecuteAlways]
    public class CentipedeGaitClock : MonoBehaviour
    {
        [Tooltip("Transform whose movement drives the gait - typically the head. The clock freezes when this isn't moving.")]
        [SerializeField] private Transform movementReference;
        [Tooltip("Distance the reference must travel to complete one full gait cycle.")]
        [SerializeField, Min(0.01f)] private float distancePerCycle = 1f;
        [Tooltip("Frame-to-frame movement below this is treated as jitter/noise, not walking.")]
        [SerializeField, Min(0f)] private float movementEpsilon = 0.0001f;

        public float Phase { get; private set; }

        private bool initialized;
        private Vector3 previousPosition;

        private void OnEnable()
        {
            initialized = false;
        }

        private void Update()
        {
            if (movementReference == null)
            {
                return;
            }

            if (!initialized)
            {
                previousPosition = movementReference.position;
                initialized = true;
                return;
            }

            float distance = Vector3.Distance(movementReference.position, previousPosition);
            if (distance > movementEpsilon)
            {
                Phase = Mathf.Repeat(Phase + distance / distancePerCycle, 1f);
            }

            previousPosition = movementReference.position;
        }
    }
}
