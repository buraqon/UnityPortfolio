using UnityEditor;
using UnityEngine;
using Centipede.Game;
using Centipede.Visual;

namespace Centipede.Editor
{
    public class CentipedeGeneratorWindow : EditorWindow
    {
        [SerializeField] private GameObject headPrefab;
        [SerializeField] private GameObject[] segmentPrefabs = new GameObject[0];
        [SerializeField] private int sectionCount = 8;
        [SerializeField] private float segmentSpacing = 0.5f;
        [SerializeField, Range(0f, 180f)] private float jointAngleLimit = 180f;
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;
        [SerializeField] private string centipedeName = "Centipede";
        [SerializeField, Min(0.01f)] private float gaitDistancePerCycle = 1f;
        [SerializeField] private float waveCyclesAlongBody = 1f;

        private SerializedObject serializedWindow;
        private SerializedProperty headPrefabProp;
        private SerializedProperty segmentPrefabsProp;
        private SerializedProperty sectionCountProp;
        private SerializedProperty segmentSpacingProp;
        private SerializedProperty jointAngleLimitProp;
        private SerializedProperty spawnPositionProp;
        private SerializedProperty centipedeNameProp;
        private SerializedProperty gaitDistancePerCycleProp;
        private SerializedProperty waveCyclesAlongBodyProp;

        [MenuItem("Game/Centipede/Generator")]
        private static void Open()
        {
            GetWindow<CentipedeGeneratorWindow>("Centipede Generator");
        }

        private void OnEnable()
        {
            serializedWindow = new SerializedObject(this);
            headPrefabProp = serializedWindow.FindProperty(nameof(headPrefab));
            segmentPrefabsProp = serializedWindow.FindProperty(nameof(segmentPrefabs));
            sectionCountProp = serializedWindow.FindProperty(nameof(sectionCount));
            segmentSpacingProp = serializedWindow.FindProperty(nameof(segmentSpacing));
            jointAngleLimitProp = serializedWindow.FindProperty(nameof(jointAngleLimit));
            spawnPositionProp = serializedWindow.FindProperty(nameof(spawnPosition));
            centipedeNameProp = serializedWindow.FindProperty(nameof(centipedeName));
            gaitDistancePerCycleProp = serializedWindow.FindProperty(nameof(gaitDistancePerCycle));
            waveCyclesAlongBodyProp = serializedWindow.FindProperty(nameof(waveCyclesAlongBody));
        }

        private void OnGUI()
        {
            serializedWindow.Update();

            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(headPrefabProp);
            EditorGUILayout.PropertyField(segmentPrefabsProp, new GUIContent("Segment Prefabs"), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sectionCountProp, new GUIContent("Section Count"));
            EditorGUILayout.PropertyField(segmentSpacingProp, new GUIContent("Segment Spacing (fallback, jointless pieces)"));
            EditorGUILayout.PropertyField(spawnPositionProp, new GUIContent("Spawn Position"));
            EditorGUILayout.PropertyField(centipedeNameProp, new GUIContent("Name"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Physics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(jointAngleLimitProp, new GUIContent("Joint Angle Limit"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gait", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(gaitDistancePerCycleProp, new GUIContent("Gait Distance Per Cycle"));
            EditorGUILayout.PropertyField(waveCyclesAlongBodyProp, new GUIContent("Wave Cycles Along Body"));

            serializedWindow.ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (headPrefab == null)
            {
                EditorGUILayout.HelpBox("Assign a head prefab.", MessageType.Info);
            }

            if (segmentPrefabs == null || segmentPrefabs.Length == 0)
            {
                EditorGUILayout.HelpBox("Assign at least one segment prefab.", MessageType.Info);
            }

            bool canGenerate = headPrefab != null && segmentPrefabs != null && segmentPrefabs.Length > 0 && sectionCount > 0;
            using (new EditorGUI.DisabledScope(!canGenerate))
            {
                if (GUILayout.Button("Generate Centipede"))
                {
                    Generate();
                }
            }
        }

        private void Generate()
        {
            var root = new GameObject(string.IsNullOrWhiteSpace(centipedeName) ? "Centipede" : centipedeName);
            Undo.RegisterCreatedObjectUndo(root, "Generate Centipede");
            root.transform.position = spawnPosition;
            root.transform.rotation = Quaternion.identity;

            Transform head = InstantiatePrefab(headPrefab, root.transform, spawnPosition, root.transform.rotation, "Head");
            Vector3 cursor = GetBackJoint(head).position;

            var segments = new Transform[sectionCount];
            for (int i = 0; i < sectionCount; i++)
            {
                GameObject prefab = segmentPrefabs[i % segmentPrefabs.Length];
                Transform segment = InstantiatePrefab(prefab, root.transform, cursor, root.transform.rotation, $"Segment_{i:00}");

                CentipedeJoint joint = segment.GetComponent<CentipedeJoint>();
                Transform front = joint != null ? joint.FrontJoint : segment;
                Transform back = joint != null ? joint.BackJoint : segment;

                Vector3 frontLocalOffset = segment.InverseTransformPoint(front.position);
                segment.position = cursor - root.transform.rotation * frontLocalOffset;

                segments[i] = segment;
                cursor = front != back ? back.position : cursor - root.transform.forward * segmentSpacing;
            }

            var controller = root.AddComponent<CentipedeController>();

            // Field names below must match CentipedeController's private serialized fields exactly -
            // SerializedProperty access is by string name and fails silently if they drift apart.
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("head").objectReferenceValue = head;
            SerializedProperty segmentsProp = controllerSo.FindProperty("segments");
            segmentsProp.arraySize = segments.Length;
            for (int i = 0; i < segments.Length; i++)
            {
                segmentsProp.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
            }
            controllerSo.FindProperty("jointAngleLimit").floatValue = jointAngleLimit;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            var gaitClock = root.AddComponent<CentipedeGaitClock>();
            var gaitClockSo = new SerializedObject(gaitClock);
            gaitClockSo.FindProperty("movementReference").objectReferenceValue = head;
            gaitClockSo.FindProperty("distancePerCycle").floatValue = gaitDistancePerCycle;
            gaitClockSo.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < segments.Length; i++)
            {
                float wavePhaseOffset = segments.Length > 0 ? (i / (float)segments.Length) * waveCyclesAlongBody : 0f;
                foreach (CentipedeLeg leg in segments[i].GetComponentsInChildren<CentipedeLeg>())
                {
                    var legSo = new SerializedObject(leg);
                    legSo.FindProperty("gaitClock").objectReferenceValue = gaitClock;
                    legSo.FindProperty("wavePhaseOffset").floatValue = wavePhaseOffset;
                    legSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        private static Transform InstantiatePrefab(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, string fallbackName)
        {
            GameObject instance = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                : new GameObject(fallbackName);

            instance.transform.SetParent(parent);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.name = prefab != null ? prefab.name : fallbackName;
            Undo.RegisterCreatedObjectUndo(instance, "Generate Centipede");
            return instance.transform;
        }

        private static Transform GetBackJoint(Transform piece)
        {
            CentipedeJoint joint = piece.GetComponent<CentipedeJoint>();
            return joint != null ? joint.BackJoint : piece;
        }
    }
}
