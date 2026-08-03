using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
[CreateAssetMenu(menuName = "Formation/ScatterFormation", fileName = "Formation_Scatter")]
public class Formation_Scatter : Formation
{
    [SerializeField]
    float _radius;
    public override Vector3 GetPosition(int postion_index, int count, Vector3 origin)
    {
        System.Random random = new System.Random();
        if (_positions.Count() < count)
        {
            float radius = _size * count / (float)(2 * Math.PI);
            _positions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = 2 * (float)Math.PI * (float) random.NextDouble();
                float distance = (float)Math.Sqrt(random.NextDouble()) * _radius;
                float x = origin.x + distance * (float)Math.Cos(angle);
                float z = origin.z + distance * (float)Math.Sin(angle);
                _positions[i] = new Vector3(x, origin.y, z);
            }
        }
        return _positions[postion_index];
    }
}
