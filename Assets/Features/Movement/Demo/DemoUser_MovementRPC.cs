using System;
using System.Collections;
using System.Collections.Generic;
using HippoLib;
using HippoLib.Movement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class DemoUser_MovementRPC : NetworkBehaviour
{
    [SerializeField] private MovementHandler movementHandler;
    [SerializeField] private LayerMask groundLayer;

    private Vector3 movemetDirection;
    private bool isJumpPressed;
    private Vector3 lookDirection;

    private float dashTimer = 1f;

    public override void OnNetworkSpawn()
    {
        //NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
    }

    public void Update()
    {
        dashTimer += Time.deltaTime;

        if (IsOwner)
        {
            movemetDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            lookDirection = GetPlayerMousePosition() - transform.position;
            isJumpPressed = Input.GetKeyDown(KeyCode.Space);
            InputServerRPC(movemetDirection, lookDirection, isJumpPressed);

            movementHandler.MoveInDirection(movemetDirection);
            movementHandler.LookInDirection(lookDirection);

            if (isJumpPressed && dashTimer >= 1)
            {

                movementHandler.AddForceMovement(new MovementHandler_ForcedMovementDemo(movementHandler,
                    lookDirection.normalized * 3, 30,
                    OnDone));

                dashTimer = 0;
            }
        }
    }

    [ServerRpc]
    public void InputServerRPC(Vector3 movement, Vector3 look, bool jump)
    {
        movementHandler.MoveInDirection(movement);
        movementHandler.LookInDirection(look);

        if (jump && dashTimer >= 1)
        {

            movementHandler.AddForceMovement(new MovementHandler_ForcedMovementDemo(movementHandler,
                look.normalized * 3, 30,
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