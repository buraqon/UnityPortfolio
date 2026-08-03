using System;
using UnityEngine;

public interface IMovable : IEntity
{
    public void AddForceMovement(Force_Movement forcedMovement, Vector3 direction, Action onMovementDone);
    public void SetSpeedMultiplier(float newValue);
    public void ResetMovement();
    public void SetGravityMultiplier(float value);
    public void ResetGravityMultiplier();
}
