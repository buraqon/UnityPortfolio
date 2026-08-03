 using System;
 using System.Collections;
 using HippoLib;
using Unity.Netcode;
using UnityEngine;

public class DemoNPC_Conjure : NetworkBehaviour, IConjureSender, IConjureReciever
{
    [SerializeField] private Conjure_Data abilityData;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private MeshRenderer meshRenderer;
    
    private float timer;

    private void Update()
    {
        if(!IsServer)
            return;
        
        timer -= Time.deltaTime;
        
        if (timer <= 0)
        {
            ShootConjure();
            timer = cooldown;
        }
    }

    private void ShootConjure()
    {     
        var param = new Conjure_Params(this, visualTransform.position, visualTransform.rotation);
        abilityData.SpawnSpell(param);
    }

    public float DeltaTime => Time.deltaTime;

    public Transform VisualTransform => visualTransform;

    public Vector3 MousePosition => transform.position + transform.forward;

    public bool IsTarget(IConjureReciever reciever)
    {
        return !ReferenceEquals(reciever, this);
    }

    public void OnSpellRecieved(Conjure conjure)
    {
        StartCoroutine(Flash());
    }
    
    private IEnumerator Flash()
    {
        meshRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        meshRenderer.material.color = Color.white;
        yield return null;
    }

    public bool IsAlive()
    {
        return true;
    }
}
