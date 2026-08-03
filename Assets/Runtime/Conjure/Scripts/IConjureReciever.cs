using System.Collections;
using System.Collections.Generic;
using HippoLib;
using UnityEngine;

public interface IConjureReciever
{
    Transform transform { get; }
    public void OnSpellRecieved(Conjure conjure);
    bool IsAlive();
}
