using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HippoLib
{
    public class AssetCreator
    {
#if UNITY_EDITOR
        public static void CreateScriptable<ScriptableType>(UnityEngine.Object caller, string typeName, Action<ScriptableType> OnObjectCreated) where ScriptableType : ScriptableObject
        {
            var scriptable = (ScriptableType)ScriptableObject.CreateInstance(typeof(ScriptableType));
            scriptable.name = caller.name + "_" + typeName;
            OnObjectCreated.Invoke(scriptable);

            var path = AssetDatabase.GetAssetPath(caller);
            path = path.Remove(path.LastIndexOf("/")) + "/" + scriptable.name + ".asset";

            AssetDatabase.CreateAsset(scriptable, path);
            EditorUtility.SetDirty(caller);
        }
        

        public static void CreatePrefabVariant(UnityEngine.Object obj, string gameObjectName, string variantName)
        {
            var path = AssetDatabase_Utility.GetAssetFolderPath(obj);
            CreatePrefabVariant(path, gameObjectName, variantName);
        }

        public static void CreatePrefabVariant(string path, string gameObjectName, string variantName)
        {
            // GameObject prefabRef = (GameObject)AssetDatabase.LoadMainAssetAtPath(path + gameObjectName);
            GameObject prefabRef = Resources.Load<GameObject>(gameObjectName);
            if (!prefabRef)
            {
                Debug.LogWarning(gameObjectName + " does not exist, create a new prefab in a Resource Folder");
                return;
            }

            GameObject instanceRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefabRef);
            GameObject pVariant = PrefabUtility.SaveAsPrefabAsset(instanceRoot, path + variantName + ".prefab");
            GameObject.DestroyImmediate(instanceRoot);
        }
#endif
    }
}