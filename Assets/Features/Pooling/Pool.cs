
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace HippoLib.Pooling
{
    public class Pool : MonoBehaviour
    {
        public static Pool instance;
        public Dictionary<GameObject, List<GameObject>> pool = new Dictionary<GameObject, List<GameObject>>();

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(gameObject);

        }

        public static GameObject GetItem(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var obj = GetItem(prefab);
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.transform.SetParent(parent);
            }
            return obj;
        }

        public static GameObject GetItem(GameObject prefab)
        {
            if (prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Prefab is null!");
#endif
                return null;
            }

            if (!instance.pool.ContainsKey(prefab))
            {
                instance.pool.Add(prefab, new List<GameObject>());
            }

            return GetItemFromPool(prefab);
        }

        private static GameObject GetItemFromPool(GameObject prefab)
        {
            GameObject obj;
            if (instance.pool[prefab].Count == 0)
            {
                obj = Instantiate(prefab);
                obj.GetComponent<Pool_Item>().Prefab = prefab;
            }
            else
            {
                obj = instance.pool[prefab].Last();
                instance.pool[prefab].Remove(obj);
                obj.SetActive(true);
            }

            return obj;
        }

        public static void PoolItem(GameObject obj)
        {
            if (obj == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Object is null!");
#endif
                return;
            }

            var poolItem = obj.GetComponent<Pool_Item>();
            if (poolItem != null)
                PoolItem(obj, poolItem.Prefab);
        }

        public static void PoolItem(GameObject obj, GameObject prefab)
        {
            if (instance.pool.ContainsKey(prefab))
            {
                if (obj.activeSelf)
                {
                    obj.SetActive(false);
                    instance.pool[prefab].Add(obj);
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning(obj + " is not in the pool! Object will be destroyed not pooled!");
#endif
                Destroy(obj);
            }
        }
    }
}
