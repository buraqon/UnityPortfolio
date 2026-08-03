using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Utility class to copy fields from one object instance to another using reflection.
/// Works regardless of the specific type - copies all matching fields by name.
/// </summary>
public static class FieldCopier
{
    public static void CopyFields(object source, object destination)
    {
        if (source == null || destination == null)
            return;
        
        Type sourceType = source.GetType();
        while (sourceType != null)
        {
            UpdateForType(source, destination, sourceType);
            sourceType = sourceType.BaseType;
        }
    }

    private static void UpdateForType(object source, object destination, Type sourceType)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        FieldInfo[] sourceFields = sourceType.GetFields(flags);
        var destFieldDict = new System.Collections.Generic.Dictionary<string, FieldInfo>();
        
        foreach (var field in sourceType.GetFields(flags))
        {
            destFieldDict[field.Name] = field;
        }

        foreach (var sourceField in sourceFields)
        {
            // Skip Unity's special fields
            if (sourceField.Name.StartsWith("m_") && 
                (sourceField.Name == "m_ObjectHideFlags" || 
                 sourceField.Name == "m_CorrespondingSourceObject" ||
                 sourceField.Name == "m_PrefabInstance" ||
                 sourceField.Name == "m_PrefabAsset" ||
                 sourceField.Name == "m_InstanceID"))
                continue;

            // Only copy serialized fields (public or [SerializeField])
            bool isSerialized = sourceField.IsPublic || 
                                sourceField.GetCustomAttribute<SerializeField>() != null;

            if (isSerialized && destFieldDict.TryGetValue(sourceField.Name, out FieldInfo destField))
            {
                if (AreTypesCompatible(sourceField.FieldType, destField.FieldType))
                {
                    try
                    {
                        object value = sourceField.GetValue(source);
                        destField.SetValue(destination, value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to copy serialized field {sourceField.Name}: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if two types are compatible for field copying.
    /// </summary>
    private static bool AreTypesCompatible(Type sourceType, Type destType)
    {
        // Exact match
        if (sourceType == destType)
            return true;

        // Inheritance check
        if (destType.IsAssignableFrom(sourceType))
            return true;

        // Nullable handling
        Type underlyingSource = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        Type underlyingDest = Nullable.GetUnderlyingType(destType) ?? destType;
        
        if (underlyingSource == underlyingDest)
            return true;

        // Value type conversion
        if (sourceType.IsValueType && destType.IsValueType)
        {
            try
            {
                Convert.ChangeType(Activator.CreateInstance(sourceType), destType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}

