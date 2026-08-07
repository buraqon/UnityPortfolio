using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HippoLib.ECSDemo
{
    public partial struct RotationSpeedSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dependency = state.Dependency;
            var job = new RotateJob { DeltaTime = SystemAPI.Time.DeltaTime };
            job.ScheduleParallel(dependency);
        }

        [BurstCompile]
        private partial struct RotateJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref LocalTransform transform, in RotationSpeed rotationSpeed)
            {
                transform = transform.RotateY(rotationSpeed.RadiansPerSecond * DeltaTime);
            }
        }
    }
}
