using System;
using UnityEngine;

public class HitBoxTest : MonoBehaviour
{
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private Transform firstTransform;
    [SerializeField] private Transform secondTransform;
    [SerializeField] private float time = 1f;
    [SerializeField] private bool move;
    
    private void Start()
    {
        hitbox.Initialize(OnHit);
    }

    private void OnHit(Collider[] colliders, int count)
    {
        Debug.Log($"Hit {count} colliders");
        for (int i = 0; i < count; i++)
        {
            Debug.Log(colliders[i].name);
        }
    }

    private void Update()
    {
        if(!move) return;
        
        float t = Mathf.PingPong(Time.time, time);
        transform.position = Vector3.Lerp(firstTransform.position, secondTransform.position, t/ time);
        transform.rotation = Quaternion.Slerp(firstTransform.rotation, secondTransform.rotation, t/ time);
    }
}