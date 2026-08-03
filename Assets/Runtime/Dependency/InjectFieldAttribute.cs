using System;

namespace HippoLib.Dependency
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class InjectFieldAttribute : Attribute
    {
    }
}