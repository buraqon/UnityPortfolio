using System;
using Unity.Netcode;
using UnityEngine;

namespace HippoLib.Conjures
{
    [CreateAssetMenu(menuName = "Conjure/Projectile", fileName = "Conjure_Data_Projectile")]
    public class Conjure_Data_Projectile : Conjure_Data
    {
        public float Speed = 10;
        public float Range = 10;
        public float Radius = 0.1f;
        public int PierceCount = 0;

        protected override void OnSpawnSpell(NetworkObject obj)
        {
            var proj = obj.GetComponent<Conjure_Projectile>();
            proj.InitProjectile(this);
        }

#if UNITY_EDITOR

        [ContextMenu("Add Object")]
        public virtual void Co_AddObject()
        {
            AssetCreator.CreatePrefabVariant(this, "Conjure_Projectile", "_Projectile");
        }
#endif
    }
}