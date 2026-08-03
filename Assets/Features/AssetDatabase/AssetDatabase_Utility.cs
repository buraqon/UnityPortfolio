using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HippoLib
{
    public class AssetDatabase_Utility : MonoBehaviour
    {
#if UNITY_EDITOR
        public static T[] GetAllInstances<T>() where T : ScriptableObject
        {
            return GetAllInstances<T> ("t:" + typeof(T)); 
        }

        
        public static T[] GetAllInstances<T>(string searchQuery) where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets(searchQuery); 
            T[] a = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++) //probably could get optimized 
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return a;
        }

        public static string GetAssetFolderPath(Object obj)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            int index = path.LastIndexOf("/");
            if (index >= 0)
                path = path.Substring(0, index + 1);

            return path;
        }

        public static T FindAndGetAsset<T>(string type, string assetName) where T : ScriptableObject
        {
            var path = FindAndGetAssetPath<T>(type, assetName);
            //Debug.Log("Getting Asset From path " + path);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        
        public static string FindAndGetAssetPath<T>(string type, string assetName) where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t: " + type + " " + assetName);
            if (guids.Length == 0)
            {
                Debug.LogError("Asset " + assetName + " not found");
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return path;
        }
#endif
    }
}