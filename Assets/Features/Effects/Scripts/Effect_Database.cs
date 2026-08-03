using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using RotaryHeart.Lib.SerializableDictionary;
using System.Linq;

namespace HippoLib.Effects
{
    public abstract class Effect_Database<TSender, TReciever> : MonoBehaviour
        where TSender : IEffectSender where TReciever : IEffectReciever
    {
        [SerializeField]
        private List<Effect_Data<TSender, TReciever>> _effectsList = new List<Effect_Data<TSender, TReciever>>();

        [SerializeField] private EffectsByID _effectsToID;

        public static Effect_Database<TSender, TReciever> Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            else if (Instance != this)
            {
#if UNITY_EDITOR
                Debug.LogWarning("This object already exists, destroying this one.");
#endif
                Destroy(gameObject);
            }
        }

        public int GetIndexFromData(Effect_Data<TSender, TReciever> effect)
        {
            if (_effectsToID.ContainsKey(effect))
                return _effectsToID[effect];

            return -1;
        }

        public Effect_Data<TSender, TReciever> GetDataFromIndex(int id)
        {
            if (id < _effectsList.Count)
                return _effectsList[id];

            return null;
        }

#if UNITY_EDITOR

        public void PopulateList<sender, reciever>() where sender : IEffectSender where reciever : IEffectReciever
        {
            _effectsList.Clear();
            _effectsToID.Clear();

            var effects =
                AssetDatabase_Utility.GetAllInstances<Effect_Data<TSender, TReciever>>("t: Effect_Data`2");
            
            Debug.Log(effects.Length);
            for (int i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                _effectsList.Add(effect);
                _effectsToID.Add(effect, i);

                EditorUtility.SetDirty(effect);
                EditorUtility.SetDirty(this);
            }
        }
#endif

        [System.Serializable]
        public class EffectsByID : SerializableDictionaryBase<Effect_Data<TSender, TReciever>, int>
        {
        }
    }
}