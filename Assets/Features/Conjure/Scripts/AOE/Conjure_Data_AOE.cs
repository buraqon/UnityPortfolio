using System;
using System.Collections;
using System.Collections.Generic;

using HippoLib;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;


namespace HippoLib.Conjures
{
    [CreateAssetMenu(menuName = "Conjure/AOE", fileName = "Conjure_Data_AOE")]
    public class Conjure_Data_AOE : Conjure_Data
    {
        public float Scale = 1;
        public float Lifetime = .2f;
        public int Ticks = 1;
        public float Delay = 0;
        public bool Following = false;
        protected override void OnSpawnSpell(NetworkObject obj)
        {
            var aoe = obj.GetComponent<Conjure_AOE>();
            aoe.InitAOE(this);
        }

        public override bool IsSpawnedOnCharacter()
        {
            if (Following)
                return true;

            return false;
        }

#if UNITY_EDITOR

        [ContextMenu("Add Object")]
        public void Co_AddObject()
        {
            AssetCreator.CreatePrefabVariant(this, "Conjure_AOE", "_AOE");
        }
#endif
    }
}