using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib
{
    public static class Vector3Extentions
    {
        public static Vector3 FlattenXZ(this Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }

        public static Vector3 FlattenXY(this Vector3 vector)
        {
            return new Vector3(vector.x, vector.y, 0f);
        }

        public static Vector3 FlattenYZ(this Vector3 vector)
        {
            return new Vector3(0f, vector.y, vector.z);
        }

        public static Vector3 NormalizeSafe(this Vector3 vector)
        {
            if (vector == Vector3.zero)
            {
                return vector;
            }
            return vector.normalized;
        }

        public static Vector3 RotateAroundY(this Vector3 vector, float angleDegrees)
        {
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(angleRadians);
            float cos = Mathf.Cos(angleRadians);
            return new Vector3(vector.x * cos - vector.z * sin, vector.y, vector.x * sin + vector.z * cos);
        }

        [Obsolete("Use ToXY, ToXZ, ToYZ instead, their naming is more clear, and you have more options, this might be removed later")]
        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.z);
        }

        public static Vector2 ToXY(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        public static Vector2 ToXZ(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.z);
        }

        public static Vector2 ToYZ(this Vector3 vector)
        {
            return new Vector2(vector.y, vector.z);
        }
    }
}