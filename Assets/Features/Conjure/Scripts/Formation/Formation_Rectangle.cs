using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using static UnityEngine.UI.Image;


[CreateAssetMenu(menuName = "Formation/RectangleFormation", fileName = "Formation_Rectangle")]
public class Formation_Rectangle : Formation
{
    [SerializeField]
    private float _ratio;

    public override Vector3 GetPosition(int postion_index, int count, Vector3 origin)
    {

        if (_positions.Count() < count)
        {
            float rowCount = (float)Math.Ceiling(Math.Sqrt((float)count / _ratio));
            float colCount = (float)Math.Ceiling((float)count / (double)rowCount);

            float startX = origin.x - (colCount - 1) * _size / 2;
            float startZ = origin.z - (rowCount - 1) * _size / 2;

            _positions = new Vector3[count];
            int index = 0;
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount && index < count; col++)
                {
                    float x = startX + col * _size;
                    float z = startZ + row * _size;
                    _positions[index] = new Vector3(x , origin.y, z);
                    index++;
                }
            }
        }
        return _positions[postion_index];
    }
}
