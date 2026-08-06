using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    public partial struct MicrobotIkSystem : ISystem
    {
        private const float TargetMoveSpeed = 1f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.TryGetSingleton<MicrobotInputState>(out var inputState);
            var toggleBase = inputState.ToggleBase;
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(false);

            var moveDelta = math.normalizesafe(inputState.MoveInput, float3.zero) * TargetMoveSpeed * SystemAPI.Time.DeltaTime;
            foreach (var ikTarget in SystemAPI.Query<RefRO<MicrobotIkTarget>>().WithAll<MicrobotTag>())
            {
                var targetEntity = ikTarget.ValueRO.TargetEntity;
                var targetTransform = transforms[targetEntity];
                targetTransform.Position += moveDelta;
                transforms[targetEntity] = targetTransform;
            }

            foreach (var (segments, ikTarget, ikState, transform) in SystemAPI
                         .Query<RefRO<MicrobotSegments>, RefRO<MicrobotIkTarget>, RefRW<MicrobotIkState>, RefRW<LocalTransform>>()
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

                var isToggle = ikState.ValueRO.AnchorInitialized && ikState.ValueRO.AnchorIsSegmentB != baseIsB;
                var needsAnchorRefresh = !ikState.ValueRO.AnchorInitialized || isToggle;
                if (needsAnchorRefresh)
                {
                    var oldAnchorPos = ikState.ValueRO.AnchorWorldPosition;
                    ikState.ValueRW.AnchorWorldPosition = ComputeTipWorldPosition(transforms, baseEntity, baseLength, transform.ValueRO);
                    ikState.ValueRW.AnchorInitialized = true;
                    ikState.ValueRW.AnchorIsSegmentB = baseIsB;

                    if (isToggle)
                    {
                        var targetEntity = ikTarget.ValueRO.TargetEntity;
                        var targetTransform = transforms[targetEntity];
                        targetTransform.Position = oldAnchorPos;
                        transforms[targetEntity] = targetTransform;
                    }

                    continue;
                }

                var anchorPos = ikState.ValueRO.AnchorWorldPosition;
                var targetPos = transforms[ikTarget.ValueRO.TargetEntity].Position;

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

        private static float3 ComputeTipWorldPosition(ComponentLookup<LocalTransform> transforms, Entity segmentEntity, float length, in LocalTransform rootTransform)
        {
            var segmentLocalRotation = transforms[segmentEntity].Rotation;
            var localOffset = math.rotate(segmentLocalRotation, new float3(0f, 0f, 1f)) * length;
            return rootTransform.Position + math.rotate(rootTransform.Rotation, localOffset);
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
