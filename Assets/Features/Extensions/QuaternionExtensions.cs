using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class QuaternionExtension
{
    public static Quaternion RotateByEulerAngles(this Quaternion quaternion, Vector3 eulerAngles)
    {
        return quaternion * Quaternion.Euler(eulerAngles);
    }

    public static Vector3 Forward(this Quaternion quaternion)
    {
        return quaternion * Vector3.forward;
    }

    public static Vector3 Right(this Quaternion quaternion)
    {
        return quaternion * Vector3.right;
    }

    public static Vector3 Up(this Quaternion quaternion)
    {
        return quaternion * Vector3.up;
    }

    public static bool Approximately(this Quaternion quaternion, Quaternion other, float tolerance = 0.0001f)
    {
        return Quaternion.Angle(quaternion, other) < tolerance;
    }
}
