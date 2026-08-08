using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.MicroBots.Blueprint
{
    public class CubeBlueprintSource : MonoBehaviour
    {
        [SerializeField] private float cubeSize = 2f;
        [SerializeField] private float segmentALength = 0.5f;
        [SerializeField] private float segmentBLength = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float spanSafety = 0.9f;
        [SerializeField] private float cornerHubRadius;
        [SerializeField] private bool drawGizmos = true;

        private StructureBlueprint _blueprint;
        private CubeWireframeSettings _builtSettings;

        public CubeWireframeSettings Settings => new CubeWireframeSettings
        {
            Center = transform.position,
            Size = cubeSize,
            Bot = new BotSpanSpec
            {
                LengthA = segmentALength,
                LengthB = segmentBLength,
                SpanSafety = spanSafety
            },
            CornerHubRadius = cornerHubRadius
        };

        public StructureBlueprint Blueprint
        {
            get
            {
                var settings = Settings;
                if (_blueprint == null || !SameSettings(_builtSettings, settings))
                {
                    Rebuild(settings);
                }

                return _blueprint;
            }
        }

        public int BotCount => Blueprint.BotCount;

        public int NodeCount => Blueprint.Nodes.Count;

        public int BotsPerEdge => CubeWireframeBuilder.BotsPerEdge(Settings);

        public float SpanPerBot => CubeWireframeBuilder.SpanPerBot(Settings);

        public void Rebuild()
        {
            Rebuild(Settings);
        }

        public bool Validate(List<string> problems)
        {
            return Blueprint.Validate(Settings.Bot, problems);
        }

        private void Awake()
        {
            Rebuild();
        }

        private void OnValidate()
        {
            cubeSize = Mathf.Max(0.01f, cubeSize);
            segmentALength = Mathf.Max(0.01f, segmentALength);
            segmentBLength = Mathf.Max(0.01f, segmentBLength);
            cornerHubRadius = Mathf.Clamp(cornerHubRadius, 0f, cubeSize * 0.49f);
        }

        private void Rebuild(CubeWireframeSettings settings)
        {
            _blueprint = CubeWireframeBuilder.Build(settings);
            _builtSettings = settings;
        }

        private static bool SameSettings(in CubeWireframeSettings a, in CubeWireframeSettings b)
        {
            return a.Size.Equals(b.Size)
                   && a.CornerHubRadius.Equals(b.CornerHubRadius)
                   && a.Bot.LengthA.Equals(b.Bot.LengthA)
                   && a.Bot.LengthB.Equals(b.Bot.LengthB)
                   && a.Bot.SpanSafety.Equals(b.Bot.SpanSafety)
                   && a.Center.Equals(b.Center);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            var blueprint = Blueprint;
            var nodeRadius = cubeSize * 0.015f;

            foreach (var link in blueprint.Links)
            {
                Gizmos.color = (link.Id & 1) == 0
                    ? new Color(0.35f, 0.8f, 1f)
                    : new Color(0.1f, 0.45f, 0.75f);
                Gizmos.DrawLine(link.AttachA, link.AttachB);
            }

            foreach (var node in blueprint.Nodes)
            {
                Gizmos.color = node.Kind == StructureNodeKind.Hub ? Color.yellow : Color.white;
                Gizmos.DrawSphere(node.Position, node.Kind == StructureNodeKind.Hub
                    ? nodeRadius * 2f
                    : nodeRadius);
            }
        }
    }
}
