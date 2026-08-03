using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HippoLib.Dependency
{
    public class DependencyProvider
    {
        private Dictionary<Type, Dependency> _dependencies = new Dictionary<Type, Dependency>();
        private Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        public DependencyProvider(DependencyCollection dependencies)
        {
            foreach (var dependency in dependencies)
            {
                _dependencies.Add(dependency.Type, dependency);
            }
        }

        public object Inject(object dependant)
        {
            Type type = dependant.GetType();
            while (type != null)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<InjectFieldAttribute>(false) == null) { continue; }

                    field.SetValue(dependant, Get(field.FieldType));
                }
                type = type.BaseType;
            }
            return dependant;
        }

        private object Get(Type type)
        {
            if (!_dependencies.ContainsKey(type))
            {
                throw new ArgumentException("Type is not a dependency: " + type.FullName);
            }

            var dependency = _dependencies[type];
            if (dependency.IsSingleton)
            {
                if (!_singletons.ContainsKey(type))
                {
                    _singletons.Add(type, dependency.Factory());
                }
                return _singletons[type];
            }
            else
            {
                return dependency.Factory();
            }
        }

        public T Get<T>()
        {
            return (T)Get(typeof(T));
        }

    }
}
