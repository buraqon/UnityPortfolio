using UnityEngine;
using UnityEngine.InputSystem;

namespace Centipede.Game
{
    // Attach to the head itself (not the CentipedeController root) - drives it car-style:
    // W moves forward along the head's current facing, A/D turn that facing left/right, and
    // each frame it hovers at a fixed offset above whatever surface is beneath it, tilting to
    // stay normal to that surface so it can drive up walls/cylinders, not just flat ground.
    public class CentipedeHeadController : MonoBehaviour
    {
        [Header("Drive")]
        [SerializeField, Min(0f)] private float moveSpeed = 3f;
        [SerializeField, Min(0f)] private float turnSpeed = 90f;

        [Header("Ground")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [Tooltip("How far above the surface the head hovers.")]
        [SerializeField, Min(0f)] private float groundOffset = 0.5f;
        [SerializeField, Min(0f)] private float groundRayHeight = 0.5f;
        [SerializeField, Min(0f)] private float groundRayDistance = 2f;
        [Tooltip("Degrees the surface alignment may turn per unit of forward movement - so driving into a wall gradually tips you onto it instead of snapping, and stopping freezes the transition mid-way.")]
        [SerializeField, Min(0f)] private float surfaceTurnRatePerDistance = 90f;

        private bool hasSurfaceUp;
        private Vector3 currentSurfaceUp = Vector3.up;

        
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // Turning and moving happen in the head's own local space so both keep working
            // sensibly once it's tilted onto a wall/ceiling by the surface alignment below.
            float turn = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            transform.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime, Space.Self);

            float distanceMoved = 0f;
            if (keyboard.wKey.isPressed)
            {
                distanceMoved = moveSpeed * Time.deltaTime;
                transform.position += transform.forward * distanceMoved;
            }

            AlignToSurface(distanceMoved);
        }

        private void AlignToSurface(float distanceMoved)
        {
            Vector3 probeUp = hasSurfaceUp ? currentSurfaceUp : transform.up;
            Vector3 origin = transform.position + probeUp * groundRayHeight;

            if (!Physics.Raycast(origin, -probeUp, out RaycastHit hit, groundRayHeight + groundRayDistance, groundLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            if (!hasSurfaceUp)
            {
                // First surface ever found - snap straight to it, nothing to transition from yet.
                currentSurfaceUp = hit.normal;
                hasSurfaceUp = true;
            }
            else if (distanceMoved > 0f)
            {
                // Only progress the transition while actually driving forward, not with the passage
                // of time - so pushing into a wall harder/longer is what rotates you onto it.
                currentSurfaceUp = Vector3.RotateTowards(currentSurfaceUp, hit.normal, surfaceTurnRatePerDistance * Mathf.Deg2Rad * distanceMoved, 0f);
            }

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, currentSurfaceUp);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(transform.up, currentSurfaceUp);
            }

            transform.position = hit.point + currentSurfaceUp * groundOffset;
            transform.rotation = Quaternion.LookRotation(forward.normalized, currentSurfaceUp);
        }
    }
}
