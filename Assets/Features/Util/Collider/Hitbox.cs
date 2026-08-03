using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private bool debug;
    [SerializeField] private float distanceDiff = 0.5f;
    [SerializeField] private float angleDiff = 10f;
    [SerializeField] private bool collidEnabled;
    [SerializeField] private UnityEvent onEnableColliders;
    [SerializeField] private UnityEvent onDisableColliders;

    private OverlapCollider overlapCollider;

    private Collider[] hitColliders = new Collider[20];
    private Action<Collider[], int> onCollision;

    private Vector3 bufferPosition;
    private Quaternion bufferRotation;

    private int currentIndex;

    public void Initialize(Action<Collider[], int> onCollision)
    {
        var colliders = GetComponents<Collider>().ToList();
        overlapCollider = new OverlapCollider(transform, colliders);

        bufferPosition = transform.position;
        bufferRotation = transform.rotation;

        this.onCollision = onCollision;
    }

    public void EnableColliders()
    {
        collidEnabled = true;
        onEnableColliders?.Invoke();
    }

    public void DisableColliders()
    {
        collidEnabled = false;
        onDisableColliders?.Invoke();
    }

    private void Update()
    {
        if (!collidEnabled)
            return;

        ClearColliders();

        int hitAmount = 0;

        var count = GetBufferCount();
        if (debug)
            Debug.Log("Buffer count: " + count);

        for (int i = 0; i < count; i++)
        {
            var newPos = Vector3.Lerp(bufferPosition, transform.position, (i + 1) / (float)(count + 1));
            var newRot = Quaternion.Slerp(bufferRotation, transform.rotation, (i + 1) / (float)(count + 1));
            hitAmount = ScanHitbox(newPos, newRot);
        }

        hitAmount = ScanHitbox(transform.position, transform.rotation);
        onCollision?.Invoke(hitColliders, hitAmount);

        // add position to buffer   
        bufferPosition = transform.position;
        bufferRotation = transform.rotation;

        if (debug)
        {
            Debug.Log("Hit amount: " + hitAmount);
            Debug.Log("==================================");
        }
    }

    private int GetBufferCount()
    {
        var distance = Vector3.Distance(transform.position, bufferPosition);
        var angle = Quaternion.Angle(transform.rotation, bufferRotation);

        if (distance > distanceDiff || angle > angleDiff)
        {
            var posCount = Mathf.CeilToInt(distance / distanceDiff);
            var rotCount = Mathf.CeilToInt(angle / angleDiff);
            return Mathf.Max(posCount, rotCount);
        }

        return 0;
    }


    private void ClearColliders()
    {
        currentIndex = 0;

        for (int i = 0; i < hitColliders.Length; i++)
        {
            hitColliders[i] = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (!debug || !collidEnabled)
            return;

        var count = GetBufferCount();
        for (int i = 0; i < count; i++)
        {
            var newPos = Vector3.Lerp(bufferPosition, transform.position, (i + 1) / (float)(count + 1));
            var newRot = Quaternion.Slerp(bufferRotation, transform.rotation, (i + 1) / (float)(count + 1));
            overlapCollider.ShowHitboxGizmos(newPos, newRot);
        }

        overlapCollider.ShowHitboxGizmos(transform.position, transform.rotation);
    }
    
    private int ScanHitbox(Vector3 hitBoxPos, Quaternion hitBoxRot)
    {
        var colliders = overlapCollider.ScanHitbox(hitBoxPos, hitBoxRot);
        
        for (int i = 0; i < colliders.Count; i++)
        {
            if (!hitColliders.Contains(colliders[i]))
            {
                hitColliders[currentIndex] = colliders[i];
                currentIndex++;
            }

            if (currentIndex + 1 >= hitColliders.Length)
                return hitColliders.Length;
        }

        return currentIndex;
    }
}