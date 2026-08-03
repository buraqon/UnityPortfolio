using System.Collections;
using System.Collections.Generic;
using HippoLib;
using HippoLib.Conjures;
using UnityEngine;
using Unity.Netcode;
using System;

public class Conjure_SampleUser : NetworkBehaviour, IConjureSender, IConjureReciever
{
    public Conjure_Data Conjure;

    public Conjure_SampleUser Reciever;

    public float DeltaTime => Time.deltaTime;

    public Transform VisualTransform => transform;

    public Vector3 MousePosition => transform.position + transform.forward;

    public List<IConjureReciever> GetAllTargets(TargetType targetType)
    {
        return new List<IConjureReciever>() { Reciever };
    }


    public bool IsTarget(IConjureReciever reciever)
    {
        if (Reciever == reciever)
            return true;

        return false;
    }

    public void OnSpellRecieved(Conjure conjure)
    {
    }

    public bool IsAlive()
    {
        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Conjure.SpawnSpell(new Conjure_Params(this, transform.position, transform.rotation));
        }
    }
}
