using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


namespace HippoLib.Conjures
{
    [CreateAssetMenu(menuName = "Conjure/HomingProjectile", fileName = "_Homing")]

    public class Conjure_Data_Projectile_Homing : Conjure_Data_Projectile
    {
#if UNITY_EDITOR

        [ContextMenu("Add Object")]
        public override void Co_AddObject()
        {
            AssetCreator.CreatePrefabVariant(this, "Conjure_HomingProjectile", "_Projectile");
        }
#endif
    }
}