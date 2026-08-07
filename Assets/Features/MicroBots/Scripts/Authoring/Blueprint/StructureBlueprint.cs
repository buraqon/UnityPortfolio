using System.Collections.Generic;
using Unity.Mathematics;

namespace HippoLib.MicroBots.Blueprint
{
    public enum StructureNodeKind
    {
        Chain,
        Hub
    }

    public struct StructureNode
    {
        public int Id;
        public float3 Position;
        public StructureNodeKind Kind;
        public int Capacity;
    }

    public struct StructureLink
    {
        public int Id;
        public int NodeA;
        public int NodeB;
        public float3 AttachA;
        public float3 AttachB;
        public float Span;
        public int SourceFeatureId;
    }

    public sealed class StructureBlueprint
    {
        private readonly List<StructureNode> _nodes = new List<StructureNode>();
        private readonly List<StructureLink> _links = new List<StructureLink>();
        private readonly List<List<int>> _incidentLinks = new List<List<int>>();

        public IReadOnlyList<StructureNode> Nodes => _nodes;
        public IReadOnlyList<StructureLink> Links => _links;
        public int BotCount => _links.Count;

        public int AddNode(float3 position, StructureNodeKind kind, int capacity)
        {
            var id = _nodes.Count;
            _nodes.Add(new StructureNode
            {
                Id = id,
                Position = position,
                Kind = kind,
                Capacity = capacity
            });
            _incidentLinks.Add(new List<int>(capacity));
            return id;
        }

        public int AddLink(int nodeA, int nodeB, float3 attachA, float3 attachB, int sourceFeatureId)
        {
            var id = _links.Count;
            _links.Add(new StructureLink
            {
                Id = id,
                NodeA = nodeA,
                NodeB = nodeB,
                AttachA = attachA,
                AttachB = attachB,
                Span = math.distance(attachA, attachB),
                SourceFeatureId = sourceFeatureId
            });
            _incidentLinks[nodeA].Add(id);
            _incidentLinks[nodeB].Add(id);
            return id;
        }

        public StructureNode GetNode(int nodeId) => _nodes[nodeId];

        public StructureLink GetLink(int linkId) => _links[linkId];

        public IReadOnlyList<int> IncidentLinks(int nodeId) => _incidentLinks[nodeId];

        public int Valence(int nodeId) => _incidentLinks[nodeId].Count;

        public int OtherNode(int linkId, int nodeId)
        {
            var link = _links[linkId];
            return link.NodeA == nodeId ? link.NodeB : link.NodeA;
        }

        public float3 AttachPointOf(int linkId, int nodeId)
        {
            var link = _links[linkId];
            return link.NodeA == nodeId ? link.AttachA : link.AttachB;
        }

        public bool Validate(BotSpanSpec bot, List<string> problems)
        {
            problems.Clear();

            var totalValence = 0;
            foreach (var node in _nodes)
            {
                var valence = Valence(node.Id);
                totalValence += valence;

                if (valence > node.Capacity)
                {
                    problems.Add($"Node {node.Id} has valence {valence} but capacity {node.Capacity}.");
                }

                if (valence == 0)
                {
                    problems.Add($"Node {node.Id} is orphaned.");
                }
            }

            if (totalValence != _links.Count * 2)
            {
                problems.Add($"Valence sum {totalValence} != 2 x link count {_links.Count}.");
            }

            foreach (var link in _links)
            {
                if (link.Span > bot.MaxSpan + 1e-4f)
                {
                    problems.Add($"Link {link.Id} spans {link.Span:F4}, over the usable {bot.MaxSpan:F4}.");
                }

                if (link.Span < bot.MinSpan - 1e-4f)
                {
                    problems.Add($"Link {link.Id} spans {link.Span:F4}, under the fold limit {bot.MinSpan:F4}.");
                }
            }

            return problems.Count == 0;
        }

        public List<int> BuildOrderFrom(int seedNodeId)
        {
            var order = new List<int>(_links.Count);
            var placed = new bool[_links.Count];
            var reached = new bool[_nodes.Count];
            var frontier = new Queue<int>();

            reached[seedNodeId] = true;
            frontier.Enqueue(seedNodeId);

            while (frontier.Count > 0)
            {
                var nodeId = frontier.Dequeue();
                foreach (var linkId in _incidentLinks[nodeId])
                {
                    if (placed[linkId])
                    {
                        continue;
                    }

                    placed[linkId] = true;
                    order.Add(linkId);

                    var next = OtherNode(linkId, nodeId);
                    if (!reached[next])
                    {
                        reached[next] = true;
                        frontier.Enqueue(next);
                    }
                }
            }

            return order;
        }
    }
}
