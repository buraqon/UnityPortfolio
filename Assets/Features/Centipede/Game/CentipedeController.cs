using System;
using UnityEngine;

namespace Centipede.Game
{
    [ExecuteAlways]
    public class CentipedeController : MonoBehaviour
    {
        [Header("Head")]
        [Tooltip("Not driven by this script - moved externally, e.g. by CentipedeHeadController or manually in the editor.")]
        [SerializeField] private Transform head;

        [Header("Body")]
        [Tooltip("Segments ordered from closest-to-head to tail.")]
        [SerializeField] private Transform[] segments = Array.Empty<Transform>();
        [Tooltip("Maximum angle, in degrees, each segment may bend away from the piece ahead of it.")]
        [SerializeField, Range(0f, 180f)] private float jointAngleLimit = 180f;

        [SerializeField, HideInInspector] private Transform[] backJoints = Array.Empty<Transform>();
        [SerializeField, HideInInspector] private Vector3[] frontLocalOffsets = Array.Empty<Vector3>();

        private void Awake()
        {
            BuildJointCache();
        }

        private void BuildJointCache()
        {
            int count = segments.Length;
            backJoints = new Transform[count];
            frontLocalOffsets = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Transform segment = segments[i];
                if (segment == null)
                {
                    continue;
                }

                CentipedeJoint joint = segment.GetComponent<CentipedeJoint>();
                Transform front = joint != null ? joint.FrontJoint : segment;
                backJoints[i] = joint != null ? joint.BackJoint : segment;
                frontLocalOffsets[i] = segment.InverseTransformPoint(front.position);
            }
        }

        private void Update()
        {
            if (head == null)
            {
                return;
            }

            if (backJoints == null || backJoints.Length != segments.Length)
            {
                BuildJointCache();
            }

            CentipedeJoint headJoint = head.GetComponent<CentipedeJoint>();
            Vector3 anchorPosition = headJoint != null ? headJoint.BackJoint.position : head.position;
            Quaternion previousRotation = head.rotation;

            for (int i = 0; i < segments.Length; i++)
            {
                Transform segment = segments[i];
                if (segment == null)
                {
                    continue;
                }

                Vector3 previousBackPosition = backJoints[i].position;
                Vector3 previousForward = previousRotation * Vector3.forward;
                Vector3 desiredForward = anchorPosition - previousBackPosition;

                desiredForward = desiredForward.sqrMagnitude > 0.0001f
                    ? desiredForward.normalized
                    : previousForward;

                float angle = Vector3.Angle(previousForward, desiredForward);
                Vector3 clampedForward = angle > jointAngleLimit
                    ? Vector3.RotateTowards(previousForward, desiredForward, jointAngleLimit * Mathf.Deg2Rad, 0f)
                    : desiredForward;

                Quaternion rotation = Quaternion.LookRotation(clampedForward, previousRotation * Vector3.up);
                Vector3 position = anchorPosition - rotation * frontLocalOffsets[i];
                segment.SetPositionAndRotation(position, rotation);

                anchorPosition = backJoints[i].position;
                previousRotation = rotation;
            }
        }

        [ContextMenu("Reset Joints To Straight Line")]
        public void ResetJointsToStraightLine()
        {
            if (head == null)
            {
                return;
            }

            CentipedeJoint headJoint = head.GetComponent<CentipedeJoint>();
            Vector3 anchorPosition = headJoint != null ? headJoint.BackJoint.position : head.position;
            Quaternion rotation = head.rotation;

            foreach (Transform segment in segments)
            {
                if (segment == null)
                {
                    continue;
                }

                CentipedeJoint joint = segment.GetComponent<CentipedeJoint>();
                Transform front = joint != null ? joint.FrontJoint : segment;
                Transform back = joint != null ? joint.BackJoint : segment;
                Vector3 frontLocalOffset = segment.InverseTransformPoint(front.position);

#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(segment, "Reset Centipede Joints");
#endif
                segment.SetPositionAndRotation(anchorPosition - rotation * frontLocalOffset, rotation);

                anchorPosition = back.position;
            }
        }
    }
}
