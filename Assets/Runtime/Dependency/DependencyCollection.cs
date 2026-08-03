using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HippoLib.Dependency
{
    public class DependencyCollection : IEnumerable<Dependency>
    {
        private readonly List<Dependency> _dependencies = new();
        public void Add(Dependency dependency) => _dependencies.Add(dependency);

        public IEnumerator<Dependency> GetEnumerator() => _dependencies.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _dependencies.GetEnumerator();
    }

    public struct Dependency
    {
        public Type Type { get; set; }
        public Func<object> Factory { get; set; }
        public bool IsSingleton { get; set; }
    }
}
