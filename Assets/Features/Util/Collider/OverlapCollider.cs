using System.Collections.Generic;
using HippoLib;
using NUnit.Framework.Interfaces;
using UnityEngine;

public class OverlapCollider
{
    private List<Collider> colliderList;

    private List<ColliderData> dataList = new();

    // private Collider[] tempColliders = new Collider[20];
    private List<Collider> resultColliders = new();

    public MeshCollider test;

    public OverlapCollider(Transform transform, List<Collider> colliderlist)
    {
        Initialize(transform, colliderlist);
    }

    private void Initialize(Transform transform, List<Collider> collidList)
    {
        colliderList = collidList;

        foreach (var collid in colliderList)
        {
            if (collid == null)
                continue;

            var collidTransform = collid.transform;

            Vector3 localPosition = transform.InverseTransformPoint(collidTransform.position);
            Quaternion localRotation = Quaternion.Inverse(transform.rotation) * collidTransform.rotation;
            float scale = collidTransform.lossyScale.x;

            var data = GetData(collid);
            data.SetTransformData(transform, localPosition, localRotation, scale);
            dataList.Add(data);
        }
    }

    public List<Collider> ScanHitbox(Vector3 hitBoxPos, Quaternion hitBoxRot)
    {
        resultColliders.Clear();

        for (int i = 0; i < colliderList.Count; i++)
        {
            var data = dataList[i];
            Vector3 worldPosition = hitBoxRot * data.localPosition + hitBoxPos;
            Quaternion worldRotation = hitBoxRot * data.localRotation;
            var tempColliders = data.Overlap(worldPosition, worldRotation, data.lossyScale);

            for (int j = 0; j < tempColliders.Count; j++)
            {
                if (tempColliders[j] == null)
                    continue;

                if (!resultColliders.Contains(tempColliders[j]))
                {
                    resultColliders.Add(tempColliders[j]);
                }
            }
        }

        return resultColliders;
    }

    public void ShowHitboxGizmos(Vector3 hitBoxPos, Quaternion hitBoxRot)
    {
        if (colliderList == null)
            return;

        var currentIndex = 0;
        for (int i = 0; i < colliderList.Count; i++)
        {
            var data = dataList[i];
            Vector3 worldPosition = hitBoxRot * data.localPosition + hitBoxPos;
            Quaternion worldRotation = hitBoxRot * data.localRotation;
            data.OnGizmos(worldPosition, worldRotation, data.lossyScale);
        }
    }

    private ColliderData GetData(Collider collid)
    {
        // add other collider types if needed
        var customData = new CustomColliderData()
        {
            offset = Vector3.zero
        };
        return customData;
    }

    private abstract class ColliderData
    {
        protected Transform myTransform;

        public Vector3 localPosition;
        public Quaternion localRotation;
        public float lossyScale;

        public Vector3 offset;

        public abstract List<Collider> Overlap(Vector3 position, Quaternion rotation, float scale);
        public abstract void OnGizmos(Vector3 position, Quaternion rotation, float scale);

        public void SetTransformData(Transform transform, Vector3 pos, Quaternion rot, float scale)
        {
            localPosition = pos;
            localRotation = rot;
            lossyScale = scale;
            myTransform = transform;

            OnSetTransformData(transform, pos, rot, scale);
        }

        protected virtual void OnSetTransformData(Transform transform, Vector3 pos, Quaternion rot, float scale)
        {
        }
    }

    // private class BoxData : ColliderData
    // {
    //     public Vector3 size;
    //
    //     public override int Overlap(Collider[] colliders, Vector3 position, Quaternion rotation, float scale)
    //     {
    //         position = rotation * offset + position;
    //         return Physics.OverlapBoxNonAlloc(position, size * scale / 2, colliders, rotation);
    //     }
    //
    //     public override void OnGizmos(Vector3 position, Quaternion rotation, float scale)
    //     {
    //         position = rotation * offset + position;
    //         Gizmos.DrawWireCube(position, size * scale);
    //     }
    // }
    //
    // private class SphereData : ColliderData
    // {
    //     public float radius;
    //
    //     public override int Overlap(Collider[] colliders, Vector3 position, Quaternion rotation, float scale)
    //     {
    //         position = rotation * offset + position;
    //         return Physics.OverlapSphereNonAlloc(position, radius * scale, colliders);
    //     }
    //
    //     public override void OnGizmos(Vector3 position, Quaternion rotation, float scale)
    //     {
    //         position = rotation * offset + position;
    //         Gizmos.DrawWireSphere(position, radius * scale);
    //     }
    // }
    //
    // private class CapsuleData : ColliderData
    // {
    //     public float radius;
    //     public float height;
    //
    //     public override int Overlap(Collider[] colliders, Vector3 position, Quaternion rotation, float scale)
    //     {
    //         // TODO : scale not added yet
    //
    //         var point0 = position + rotation * offset + Vector3.up * height / 2;
    //         var point1 = position + rotation * offset - Vector3.up * height / 2;
    //
    //         return Physics.OverlapCapsuleNonAlloc(point0, point1, radius, colliders);
    //     }
    //
    //     public override void OnGizmos(Vector3 position, Quaternion rotation, float scale)
    //     {
    //     }
    // }


    private class CustomColliderData : ColliderData
    {
        CollisionRelay collisionRelay;

        List<Collider> colliderList = new List<Collider>();

        protected override void OnSetTransformData(Transform transform, Vector3 pos, Quaternion rot, float scale)
        {
            collisionRelay = myTransform.GetComponent<CollisionRelay>();
            if (collisionRelay == null)
                collisionRelay = myTransform.gameObject.AddComponent<CollisionRelay>();

            collisionRelay.EnterTrigger = OnEnterTrigger;
            // collisionRelay.StayTrigger = OnStayTrigger;
            collisionRelay.ExitTrigger = OnExitTrigger;
        }

        // private void OnStayTrigger(Collider obj)
        // {
        //     if (!colliderList.Contains(obj))
        //     {
        //         colliderList.Add(obj);
        //     }
        // }


        private void OnEnterTrigger(Collider obj)
        {
            colliderList.Add(obj);
        }

        private void OnExitTrigger(Collider obj)
        {
            colliderList.Remove(obj);
        }

        public override List<Collider> Overlap(Vector3 position, Quaternion rotation, float scale)
        {
            return colliderList;
        }

        public override void OnGizmos(Vector3 position, Quaternion rotation, float scale)
        {
        }

        // public override bool IsCollidDisabled()
        // {
        //     return false;
        // }
    }
}