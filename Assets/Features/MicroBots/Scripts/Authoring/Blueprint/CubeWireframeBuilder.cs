using Unity.Mathematics;

namespace HippoLib.MicroBots.Blueprint
{
    public struct CubeWireframeSettings
    {
        public float3 Center;
        public float Size;
        public BotSpanSpec Bot;
        public float CornerHubRadius;

        public static CubeWireframeSettings Default => new CubeWireframeSettings
        {
            Center = float3.zero,
            Size = 2f,
            Bot = BotSpanSpec.Default,
            CornerHubRadius = 0f
        };

        public float UsableEdgeLength => Size - 2f * CornerHubRadius;
    }

    public static class CubeWireframeBuilder
    {
        public const int CornerCount = 8;
        public const int EdgeCount = 12;
        public const int CornerCapacity = 3;
        public const int ChainCapacity = 2;

        private static readonly int2[] Edges = BuildEdgeTable();

        public static float3 CornerPosition(float3 center, float size, int cornerIndex)
        {
            var half = size * 0.5f;
            return center + new float3(
                ((cornerIndex >> 0) & 1) == 0 ? -half : half,
                ((cornerIndex >> 1) & 1) == 0 ? -half : half,
                ((cornerIndex >> 2) & 1) == 0 ? -half : half);
        }

        public static int2 EdgeCorners(int edgeIndex) => Edges[edgeIndex];

        public static int BotsPerEdge(CubeWireframeSettings settings)
        {
            return settings.Bot.BotsToSpan(settings.UsableEdgeLength);
        }

        public static float SpanPerBot(CubeWireframeSettings settings)
        {
            return settings.UsableEdgeLength / BotsPerEdge(settings);
        }

        public static int TotalBots(CubeWireframeSettings settings)
        {
            return EdgeCount * BotsPerEdge(settings);
        }

        public static int TotalNodes(CubeWireframeSettings settings)
        {
            return CornerCount + EdgeCount * (BotsPerEdge(settings) - 1);
        }

        public static StructureBlueprint Build(CubeWireframeSettings settings)
        {
            var blueprint = new StructureBlueprint();

            for (var corner = 0; corner < CornerCount; corner++)
            {
                blueprint.AddNode(
                    CornerPosition(settings.Center, settings.Size, corner),
                    StructureNodeKind.Hub,
                    CornerCapacity);
            }

            var usableLength = settings.UsableEdgeLength;
            var botsPerEdge = BotsPerEdge(settings);
            var span = usableLength / botsPerEdge;

            for (var edgeIndex = 0; edgeIndex < EdgeCount; edgeIndex++)
            {
                var corners = Edges[edgeIndex];
                var from = blueprint.GetNode(corners.x).Position;
                var to = blueprint.GetNode(corners.y).Position;
                var direction = math.normalize(to - from);
                var chainStart = from + direction * settings.CornerHubRadius;

                var previousNode = corners.x;
                var previousAttach = chainStart;

                for (var step = 1; step < botsPerEdge; step++)
                {
                    var position = chainStart + direction * (span * step);
                    var nodeId = blueprint.AddNode(position, StructureNodeKind.Chain, ChainCapacity);
                    blueprint.AddLink(previousNode, nodeId, previousAttach, position, edgeIndex);
                    previousNode = nodeId;
                    previousAttach = position;
                }

                blueprint.AddLink(
                    previousNode,
                    corners.y,
                    previousAttach,
                    chainStart + direction * usableLength,
                    edgeIndex);
            }

            return blueprint;
        }

        private static int2[] BuildEdgeTable()
        {
            var edges = new int2[EdgeCount];
            var count = 0;

            for (var corner = 0; corner < CornerCount; corner++)
            {
                for (var axis = 0; axis < 3; axis++)
                {
                    if (((corner >> axis) & 1) == 0)
                    {
                        edges[count++] = new int2(corner, corner | (1 << axis));
                    }
                }
            }

            return edges;
        }
    }
}
