using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


namespace HippoLib.Conjures
{
    public class Conjure_Multiple : Conjure
    {
        private Formation _formation;
        private int _unitCount;
        private float _spawnTime;
        private Conjure_Data _itemToSpawn;
        private float _timer = 0;
        private int _currentIndex = 0;


        public void InitSpawn(Formation formation, Conjure_Data itemToSpawn, int unitCount, float spawn_time)
        {
            _formation = formation;
            _unitCount = unitCount;
            _spawnTime = spawn_time;
            _itemToSpawn = itemToSpawn;

            formation.ResetPositions();

            _currentIndex = 0;
        }

        protected override void ConjureSpawned()
        {
        }

        protected override void ConjureUpdate()
        {
            if (!IsServer) return;


            if (_timer >= _spawnTime)
            {
                _timer = 0;
                var spawn_position = _formation.GetPosition(_currentIndex, _unitCount, transform.position);
                var spawn_params = new Conjure_Params(Caster, spawn_position, Quaternion.identity);
                _itemToSpawn.SpawnSpell(spawn_params);
                _currentIndex++;
                // spawn unit

            }
            _timer += Caster.DeltaTime;

            if (_currentIndex >= _unitCount)
                EndLife();
        }
    }
}