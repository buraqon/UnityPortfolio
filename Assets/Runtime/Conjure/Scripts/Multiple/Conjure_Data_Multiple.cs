
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


namespace HippoLib.Conjures
{
    [CreateAssetMenu(menuName = "Conjure/Multiple", fileName = "Conjure_Data_Spawn")]

    public class Conjure_Data_Multiple : Conjure_Data
    {
        [SerializeField]
        Conjure_Data _itemToSpawn;
        [SerializeField]
        private int _spawnCount;
        [SerializeField]
        private Formation _formation;
        [SerializeField]
        private float _spawnTimer;

        protected override void OnSpawnSpell(NetworkObject obj)
        {
            var unit = obj.GetComponent<Conjure_Multiple>();
            unit.InitSpawn(_formation, _itemToSpawn, _spawnCount, _spawnTimer);
        }
    }
}