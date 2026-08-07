using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    public partial struct MicrobotIkSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            SystemAPI.TryGetSingleton<MicrobotInputState>(out var inputState);
            var toggleBase = inputState.ToggleBase;
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(false);

            foreach (var (segments, ikTargets, ikState, transform) in SystemAPI
                         .Query<RefRO<MicrobotSegments>, RefRO<MicrobotIkTargets>, RefRW<MicrobotIkState>, RefRW<LocalTransform>>()
                         .WithAll<MicrobotTag>())
            {
                if (toggleBase)
                {
                    ikState.ValueRW.BaseIsSegmentB = !ikState.ValueRO.BaseIsSegmentB;
                }

                var baseIsB = ikState.ValueRO.BaseIsSegmentB;
                var baseEntity = baseIsB ? segments.ValueRO.SegmentBEntity : segments.ValueRO.SegmentAEntity;
                var baseLength = baseIsB ? segments.ValueRO.LengthB : segments.ValueRO.LengthA;
                var endEntity = baseIsB ? segments.ValueRO.SegmentAEntity : segments.ValueRO.SegmentBEntity;
                var endLength = baseIsB ? segments.ValueRO.LengthA : segments.ValueRO.LengthB;

                var anchorEntity = baseIsB ? ikTargets.ValueRO.TargetBEntity : ikTargets.ValueRO.TargetAEntity;
                var freeEntity = baseIsB ? ikTargets.ValueRO.TargetAEntity : ikTargets.ValueRO.TargetBEntity;

                var anchorPos = transforms[anchorEntity].Position;
                var targetPos = transforms[freeEntity].Position;

                var toTargetHorizontal = targetPos - anchorPos;
                toTargetHorizontal.y = 0f;
                var forward = math.normalizesafe(toTargetHorizontal, new float3(0f, 0f, 1f));

                var rootWorldPos = SolveElbowPlanar(anchorPos, targetPos, forward, baseLength, endLength);
                var rootRotation = quaternion.LookRotationSafe(forward, math.up());

                transform.ValueRW.Position = rootWorldPos;
                transform.ValueRW.Rotation = rootRotation;

                var inverseRootRotation = math.inverse(rootRotation);
                var toAnchorLocal = math.rotate(inverseRootRotation, anchorPos - rootWorldPos);
                var toTargetLocal = math.rotate(inverseRootRotation, targetPos - rootWorldPos);

                SetSegmentRotation(transforms, baseEntity, LocalDirectionRotation(toAnchorLocal));
                SetSegmentRotation(transforms, endEntity, LocalDirectionRotation(toTargetLocal));
            }
        }

        private static float3 SolveElbowPlanar(float3 anchorPos, float3 targetPos, float3 forward, float l0, float l1)
        {
            var relative = targetPos - anchorPos;
            var planeX = math.dot(relative, forward);
            var planeY = relative.y;

            var rawDist = math.sqrt(planeX * planeX + planeY * planeY);
            var dist = math.clamp(rawDist, math.abs(l0 - l1) + 0.001f, l0 + l1 - 0.001f);

            var hasDirection = rawDist > 0.0001f;
            var dirX = hasDirection ? planeX / rawDist : 1f;
            var dirY = hasDirection ? planeY / rawDist : 0f;

            var perpX = -dirY;
            var perpY = dirX;
            if (perpY < 0f)
            {
                perpX = -perpX;
                perpY = -perpY;
            }

            var cosAngle = math.clamp((l0 * l0 + dist * dist - l1 * l1) / (2f * l0 * dist), -1f, 1f);
            var angle = math.acos(cosAngle);

            var bendX = dirX * math.cos(angle) + perpX * math.sin(angle);
            var bendY = dirY * math.cos(angle) + perpY * math.sin(angle);

            return anchorPos + forward * (bendX * l0) + new float3(0f, bendY * l0, 0f);
        }

        private static quaternion LocalDirectionRotation(float3 localDirection)
        {
            return quaternion.LookRotationSafe(math.normalizesafe(localDirection, new float3(0f, 0f, 1f)), new float3(0f, 1f, 0f));
        }

        private static void SetSegmentRotation(ComponentLookup<LocalTransform> transforms, Entity segmentEntity, quaternion rotation)
        {
            var segmentTransform = transforms[segmentEntity];
            segmentTransform.Rotation = rotation;
            transforms[segmentEntity] = segmentTransform;
        }
    }
}
