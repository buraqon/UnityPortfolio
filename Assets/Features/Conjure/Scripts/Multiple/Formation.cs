
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Formation: ScriptableObject
{
    [SerializeField]
    protected Vector3[] _positions;

    [SerializeField]
    protected float _size;

    public virtual Vector3 GetPosition(int position_index, int count, Vector3 origin) { return new Vector3(); }
    public void ResetPositions()
    {
        _positions = new Vector3[0];
    }

}
