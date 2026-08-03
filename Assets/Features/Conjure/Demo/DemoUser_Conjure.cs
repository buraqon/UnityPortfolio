using System.Collections.Generic;
using HippoLib;
using HippoLib.Conjures;
using HippoLib.Movement;
using Unity.Netcode;
using UnityEngine;
public class DemoUser_Conjure : NetworkBehaviour, IConjureReciever, IConjureSender
{
    [SerializeField] private MovementHandler movementHandler;
    [SerializeField] private Conjure_Data conjureData;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private float cooldown = 2f;

    private float timer;

    private NetworkVariable<Vector3> direction = new(default, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> conjure = new(default, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);


    public float DeltaTime => Time.deltaTime;

    public Transform VisualTransform => visualTransform;

    public Vector3 MousePosition => transform.position + transform.forward;

    private void Start()
    {

    }

    public void Update()
    {
        if (IsOwner)
        {
            direction.Value = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            conjure.Value = Input.GetKey(KeyCode.Alpha1);
        }
        
        if(!IsOwner && !IsServer)
            return;

        movementHandler.MoveInDirection(direction.Value);
        if (conjure.Value && timer <= 0)
        {
            var param = new Conjure_Params(this, visualTransform.position, visualTransform.rotation);
            conjureData.SpawnSpell(param);
            timer = cooldown;
        }
        if(timer > 0)
            timer -= Time.deltaTime;
    }

    public void OnSpellRecieved(Conjure conjure)
    {
        
    }

    public bool IsAlive()
    {
        return true;
    }

    public bool IsTarget(IConjureReciever reciever)
    {
        return true;
    }
}
