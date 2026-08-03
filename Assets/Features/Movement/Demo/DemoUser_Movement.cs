using System;
using System.Collections;
using System.Collections.Generic;
using HippoLib;
using HippoLib.Movement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class DemoUser_Movement : NetworkBehaviour
{
    [SerializeField] private MovementHandler movementHandler;
    [SerializeField] private LayerMask groundLayer;

    private NetworkVariable<Vector3> movemetDirection = new(default, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> isJumpPressed = new(default, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private NetworkVariable<Vector3> lookDirection = new NetworkVariable<Vector3>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    private float dashTimer;

    public void Update()
    {
        dashTimer += Time.deltaTime;

        if (IsOwner)
        {
            movemetDirection.Value = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            isJumpPressed.Value = Input.GetKey(KeyCode.Space);
            lookDirection.Value = GetPlayerMousePosition() - transform.position;
        }

        movementHandler.MoveInDirection(movemetDirection.Value);
        movementHandler.LookInDirection(lookDirection.Value);

        if (isJumpPressed.Value && dashTimer >= 1)
        {
            movementHandler.AddForceMovement(new MovementHandler_ForcedMovementDemo(movementHandler,
                lookDirection.Value.normalized * 3, 30,
                OnDone));

            dashTimer = 0;
        }
    }

    public Vector3 GetPlayerMousePosition()
    {
        Vector2 localMousePosition = Input.mousePosition.ToXY();
        Ray ray = Camera.main.ScreenPointToRay(localMousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, groundLayer) && IsOwner)
            return hit.point;
#if UNITY_EDITOR
        Debug.DrawLine(ray.origin, hit.point, Color.red);
#endif
        return Vector3.zero;
    }

    private void OnDone()
    {
    }
}