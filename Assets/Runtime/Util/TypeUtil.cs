using System;
using System.Linq;

namespace HippoLib.Runtime.Util
{
    public class TypeUtil 
    {
        public static Type[] GetDerivedTypes(Type baseType)
        {
            return System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => baseType.IsAssignableFrom(p) && p != baseType)
                .ToArray();
        }
    }
}