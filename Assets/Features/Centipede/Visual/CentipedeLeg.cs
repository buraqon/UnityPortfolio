using UnityEngine;

namespace Centipede.Visual
{
    // Drives a Two Bone IK Constraint's Target transform (Animation Rigging package) - attach
    // this to that target object, not to the leg mesh/bones themselves.
    [ExecuteAlways]
    public class CentipedeLeg : MonoBehaviour
    {
        [Tooltip("Child of the hip marking where this leg naturally rests when it can't find ground; also used as the ground-probe origin.")]
        [SerializeField] private Transform restTarget;

        [Header("Gait")]
        [Tooltip("Shared clock this leg reads from. If left unset, falls back to the nearest CentipedeGaitClock up the hierarchy (e.g. the whole centipede's root) at runtime.")]
        [SerializeField] private CentipedeGaitClock gaitClock;
        [Tooltip("Where in the shared cycle (0-1) this leg's swing window begins, from left/right alternation on its own segment (e.g. 0 and 0.5).")]
        [SerializeField, Range(0f, 1f)] private float sidePhaseOffset = 0f;
        [Tooltip("Additional phase offset from this leg's position along the body, so the wave travels head-to-tail. Left at 0 by default; the generator assigns it per segment.")]
        [SerializeField] private float wavePhaseOffset = 0f;
        [Tooltip("Fraction of the cycle (0-1) this leg spends swinging; the rest is stance (foot planted).")]
        [SerializeField, Range(0.05f, 0.95f)] private float swingFraction = 0.4f;

        [Header("Ground")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0f)] private float groundRayHeight = 0.5f;
        [SerializeField, Min(0f)] private float groundRayDistance = 1f;

        [Header("Step")]
        [Tooltip("How far ahead (walking forward) or behind (walking backward) of the rest position this leg reaches.")]
        [SerializeField, Min(0f)] private float stepDistance = 0.5f;
        [SerializeField, Min(0f)] private float stepHeight = 0.3f;

        private bool initialized;
        private bool wasInSwing;
        private Vector3 previousRestPosition;
        private Vector3 plantedFootPosition;
        private Vector3 stepStartPosition;
        private Vector3 stepEndPosition;

        private void OnEnable()
        {
            initialized = false;

            if (gaitClock == null)
            {
                gaitClock = GetComponentInParent<CentipedeGaitClock>();
            }
        }

        private void LateUpdate()
        {
            if (restTarget == null || gaitClock == null)
            {
                return;
            }

            bool isGrounded = TryGetGroundPoint(out Vector3 groundPoint);

            if (!initialized)
            {
                plantedFootPosition = isGrounded ? groundPoint : restTarget.position;
                stepEndPosition = plantedFootPosition;
                previousRestPosition = restTarget.position;
                wasInSwing = false;
                initialized = true;
            }

            if (!isGrounded)
            {
                // Nothing within reach - don't step, just hold at the rest pose until grounded again.
                // Resetting wasInSwing means the next swing window (once regrounded) is treated as fresh.
                plantedFootPosition = restTarget.position;
                stepEndPosition = plantedFootPosition;
                previousRestPosition = restTarget.position;
                wasInSwing = false;
                transform.position = plantedFootPosition;
                return;
            }

            // Reach the foot ahead of the rest position when walking forward and trail it behind
            // when walking backward, instead of always aiming straight under the hip.
            float travelDot = Vector3.Dot(restTarget.position - previousRestPosition, restTarget.forward);
            float travelDirection = travelDot > 0.0001f ? 1f : (travelDot < -0.0001f ? -1f : 0f);
            Vector3 idealPosition = groundPoint + restTarget.forward * (stepDistance * travelDirection);
            previousRestPosition = restTarget.position;

            float localPhase = Mathf.Repeat(gaitClock.Phase - sidePhaseOffset - wavePhaseOffset, 1f);
            bool isInSwing = localPhase < swingFraction;

            if (isInSwing && !wasInSwing)
            {
                // Entering a new swing window - lock the start/end so the target doesn't shift mid-swing.
                stepStartPosition = plantedFootPosition;
                stepEndPosition = idealPosition;
            }

            if (isInSwing)
            {
                float t = Mathf.Clamp01(localPhase / swingFraction);
                Vector3 flatPosition = Vector3.Lerp(stepStartPosition, stepEndPosition, t);
                plantedFootPosition = flatPosition + restTarget.up * (Mathf.Sin(t * Mathf.PI) * stepHeight);
            }
            else
            {
                plantedFootPosition = stepEndPosition;
            }

            wasInSwing = isInSwing;
            transform.position = plantedFootPosition;
        }

        private bool TryGetGroundPoint(out Vector3 groundPoint)
        {
            // Cast opposite the hip's own current up (not world down) so footing still works once
            // the body tilts onto a wall or cylinder - segments inherit their up from the head via
            // CentipedeController's chain, so restTarget.up already reflects that.
            Vector3 up = restTarget.up;
            Vector3 origin = restTarget.position + up * groundRayHeight;

            if (Physics.Raycast(origin, -up, out RaycastHit hit, groundRayHeight + groundRayDistance, groundLayers, QueryTriggerInteraction.Ignore))
            {
                groundPoint = hit.point;
                return true;
            }

            groundPoint = default;
            return false;
        }
    }
}
