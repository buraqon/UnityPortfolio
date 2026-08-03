using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.Dependency
{
    [DefaultExecutionOrder(-1)]
    public abstract class DependencyContext : MonoBehaviour
    {
        protected DependencyCollection dependenciesCollection = new DependencyCollection();
        private DependencyProvider dependenciesProvider;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Setup();

            dependenciesProvider = new DependencyProvider(dependenciesCollection);

            var children = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var child in children)
            {
                dependenciesProvider.Inject(child);
            }

            Configure();
        }

        protected abstract void Setup();

        protected abstract void Configure();
    }
}