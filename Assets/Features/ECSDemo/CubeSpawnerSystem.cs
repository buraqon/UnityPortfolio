using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.InputSystem;

namespace HippoLib.ECSDemo
{
    public partial struct CubeSpawnerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var keyboard = Keyboard.current;
            var spacePressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
            var now = SystemAPI.Time.ElapsedTime;

            foreach (var (spawner, cubes, entity) in
                     SystemAPI.Query<RefRW<CubeSpawner>, DynamicBuffer<SpawnedCube>>().WithEntityAccess())
            {
                var data = spawner.ValueRO;
                var timerDue = data.Interval > 0f && now >= data.NextSpawnTime;

                if (!spacePressed && !timerDue)
                    continue;

                for (var i = 0; i < data.Count; i++)
                {
                    var instance = state.EntityManager.Instantiate(data.Prefab);
                    cubes.Add(new SpawnedCube { Value = instance });
                }

                RelayoutLattice(ref state, cubes, data.Spacing);

                if (timerDue)
                    spawner.ValueRW.NextSpawnTime = now + data.Interval;
            }
        }

        private static void RelayoutLattice(ref SystemState state, DynamicBuffer<SpawnedCube> cubes, float spacing)
        {
            var sideLength = 1;
            while (sideLength * sideLength * sideLength < cubes.Length)
                sideLength++;

            var center = new float3(sideLength - 1) * spacing * 0.5f;

            for (var i = 0; i < cubes.Length; i++)
            {
                var x = i % sideLength;
                var y = (i / sideLength) % sideLength;
                var z = i / (sideLength * sideLength);

                var position = new float3(x, y, z) * spacing - center + new float3(0f, 0f, 5f);
                var transform = state.EntityManager.GetComponentData<LocalTransform>(cubes[i].Value);
                transform.Position = position;
                state.EntityManager.SetComponentData(cubes[i].Value, transform);
            }
        }
    }
}
