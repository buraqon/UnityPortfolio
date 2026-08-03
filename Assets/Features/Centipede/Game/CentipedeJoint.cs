using UnityEngine;

namespace Centipede.Game
{
    public class CentipedeJoint : MonoBehaviour
    {
        [Tooltip("Where this piece connects to the piece ahead of it (toward the head). Leave unset on the head. Falls back to this transform if unset.")]
        [SerializeField] private Transform frontJoint;

        [Tooltip("Where the next piece behind connects to this one (toward the tail). Leave unset on the tail-most segment. Falls back to this transform if unset.")]
        [SerializeField] private Transform backJoint;

        public Transform FrontJoint => frontJoint != null ? frontJoint : transform;
        public Transform BackJoint => backJoint != null ? backJoint : transform;
    }
}
