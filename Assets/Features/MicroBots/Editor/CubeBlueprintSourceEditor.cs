using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HippoLib.MicroBots.Blueprint
{
    [CustomEditor(typeof(CubeBlueprintSource))]
    public class CubeBlueprintSourceEditor : UnityEditor.Editor
    {
        private readonly List<string> _problems = new List<string>();

        public override bool RequiresConstantRepaint() => true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var source = (CubeBlueprintSource)target;
            var settings = source.Settings;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Bots", source.BotCount);
                EditorGUILayout.IntField("Bots Per Edge", source.BotsPerEdge);
                EditorGUILayout.IntField("Nodes", source.NodeCount);
                EditorGUILayout.IntField("Corner Hubs", CountHubs(source.Blueprint));
                EditorGUILayout.FloatField("Span Per Bot", source.SpanPerBot);
                EditorGUILayout.FloatField("Usable Span", settings.Bot.MaxSpan);
                EditorGUILayout.FloatField("Elbow Angle", settings.Bot.ElbowAngleDegrees(source.SpanPerBot));
            }

            EditorGUILayout.Space();

            if (source.BotCount == 0)
            {
                EditorGUILayout.HelpBox(
                    "These settings produce an empty blueprint.",
                    MessageType.Warning);
            }
            else if (source.Validate(_problems))
            {
                EditorGUILayout.HelpBox("Blueprint is valid.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(string.Join("\n", _problems), MessageType.Error);
            }
        }

        private static int CountHubs(StructureBlueprint blueprint)
        {
            var hubs = 0;
            foreach (var node in blueprint.Nodes)
            {
                if (node.Kind == StructureNodeKind.Hub)
                {
                    hubs++;
                }
            }

            return hubs;
        }
    }
}
