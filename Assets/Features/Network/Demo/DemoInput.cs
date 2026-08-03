using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DemoInput : NetworkedInput
{
    [SerializeField] private DemoInputActions inputActions;
    [SerializeField] private DemoInputActions.DemoPlayerActions playerActions;

    public void Start()
    {
        inputActions = new DemoInputActions();
        playerActions = inputActions.DemoPlayer;
        playerActions.Enable();

        var actionList = new List<InputAction>
        {
            playerActions.Move,
        };
        Initialize(actionList);
    }

    public Vector2 GetMovement()
    {
        var movement = Get<Vector2>(playerActions.Move);
        return movement;
    }
}
