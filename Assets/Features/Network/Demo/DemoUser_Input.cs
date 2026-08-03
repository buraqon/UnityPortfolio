using HippoLib.Movement;
using HippoLib;
using System.Globalization;
using System;
using Unity.Netcode;
using UnityEngine;

public class DemoUser_Input : NetworkBehaviour
{
    [SerializeField] private MovementHandler movementHandler;
    [SerializeField] private DemoInput input;

    private void Start()
    {

    }

    public void Update()
    {
        if (!IsOwner && !IsServer)
            return;

        var move = input.GetMovement();
        movementHandler.MoveInDirection(new Vector3(move.x, 0, move.y));
    }
}
