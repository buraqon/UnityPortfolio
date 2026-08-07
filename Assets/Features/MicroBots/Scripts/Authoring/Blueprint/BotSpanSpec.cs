using Unity.Mathematics;

namespace HippoLib.MicroBots.Blueprint
{
    public struct BotSpanSpec
    {
        public float LengthA;
        public float LengthB;
        public float SpanSafety;

        public static BotSpanSpec Default => new BotSpanSpec
        {
            LengthA = 0.5f,
            LengthB = 0.5f,
            SpanSafety = 0.9f
        };

        public float Reach => LengthA + LengthB;

        // Never span the full reach: a fully extended 2-bone chain is an IK singularity
        // (bend plane undefined, solver clamps and jitters at the boundary).
        public float MaxSpan => Reach * SpanSafety;

        public float MinSpan => math.abs(LengthA - LengthB);

        public bool CanSpan(float distance)
        {
            return distance >= MinSpan && distance <= MaxSpan;
        }

        public float ElbowAngleRadians(float span)
        {
            var cosine = (LengthA * LengthA + LengthB * LengthB - span * span)
                         / (2f * LengthA * LengthB);
            return math.acos(math.clamp(cosine, -1f, 1f));
        }

        public float ElbowAngleDegrees(float span)
        {
            return math.degrees(ElbowAngleRadians(span));
        }

        public int BotsToSpan(float distance)
        {
            if (distance <= MaxSpan)
            {
                return 1;
            }

            // Epsilon absorbs float error so an exactly-divisible run doesn't gain a spurious bot.
            return (int)math.ceil(distance / MaxSpan - 1e-5f);
        }
    }
}
