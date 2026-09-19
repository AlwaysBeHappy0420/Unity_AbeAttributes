using System;
using System.Collections.Generic;
using System.Reflection;

namespace AbeAttributes.Editor
{
    internal static class AbeReflectionUtility
    {
        // ================================================================
        // All Members
        // ================================================================

        public static IEnumerable<FieldInfo> GetAllFields(
            object target,
            Func<FieldInfo, bool> predicate = null)
        {
            if (target == null)
            {
                yield break;
            }

            IReadOnlyList<FieldInfo> fields =
                AbeMemberScanner.GetAllFields(
                    target.GetType());

            for (int i = 0;
                 i < fields.Count;
                 i++)
            {
                FieldInfo field =
                    fields[i];

                if (predicate == null ||
                    predicate(field))
                {
                    yield return field;
                }
            }
        }

        public static IEnumerable<PropertyInfo> GetAllProperties(
            object target,
            Func<PropertyInfo, bool> predicate = null)
        {
            if (target == null)
            {
                yield break;
            }

            IReadOnlyList<PropertyInfo> properties =
                AbeMemberScanner.GetAllProperties(
                    target.GetType());

            for (int i = 0;
                 i < properties.Count;
                 i++)
            {
                PropertyInfo property =
                    properties[i];

                if (predicate == null ||
                    predicate(property))
                {
                    yield return property;
                }
            }
        }

        public static IEnumerable<MethodInfo> GetAllMethods(
            object target,
            Func<MethodInfo, bool> predicate = null)
        {
            if (target == null)
            {
                yield break;
            }

            IReadOnlyList<MethodInfo> methods =
                AbeMemberScanner.GetAllMethods(
                    target.GetType());

            for (int i = 0;
                 i < methods.Count;
                 i++)
            {
                MethodInfo method =
                    methods[i];

                if (predicate == null ||
                    predicate(method))
                {
                    yield return method;
                }
            }
        }

        // ================================================================
        // Member Lookup
        // ================================================================

        public static FieldInfo FindField(
            Type type,
            string name)
        {
            return AbeMemberScanner.FindField(
                type,
                name);
        }

        public static PropertyInfo FindProperty(
            Type type,
            string name)
        {
            return AbeMemberScanner.FindProperty(
                type,
                name);
        }

        public static MethodInfo GetMethod(
            Type type,
            string name)
        {
            return AbeMemberScanner.GetMethod(
                type,
                name);
        }

        public static MethodInfo GetMethod(
            object target,
            string name)
        {
            if (target == null)
            {
                return null;
            }

            return AbeMemberScanner.GetMethod(
                target.GetType(),
                name);
        }

        public static MemberInfo FindFieldOrProperty(
            Type type,
            string name)
        {
            return AbeMemberScanner
                .FindFieldOrProperty(
                    type,
                    name);
        }

        public static Type GetMemberType(
            MemberInfo member)
        {
            return AbeMemberAccessor
                .GetMemberType(
                    member);
        }

        // ================================================================
        // Direct Get / Set
        // ================================================================

        public static object GetValue(
            object target,
            MemberInfo member)
        {
            return AbeMemberAccessor.GetValue(
                target,
                member);
        }

        public static bool SetValue(
            object target,
            MemberInfo member,
            object value)
        {
            return AbeMemberAccessor.SetValue(
                target,
                member,
                value);
        }

        // ================================================================
        // Property Path Get
        // ================================================================

        public static object GetValue(
            object target,
            string propertyPath)
        {
            return AbePropertyPathUtility.GetValue(
                target,
                propertyPath);
        }

        public static object GetParentObject(
            object target,
            string propertyPath)
        {
            return AbePropertyPathUtility
                .GetParentObject(
                    target,
                    propertyPath);
        }

        // ================================================================
        // Property Path Set
        // ================================================================

        public static bool SetValue(
            object target,
            string propertyPath,
            object value)
        {
            return AbePropertyPathUtility
                .SetValue(
                    target,
                    propertyPath,
                    value);
        }

        // ================================================================
        // Member Value
        // ================================================================

        public static object GetMemberValue(
            object target,
            string memberName)
        {
            return AbeMemberAccessor
                .GetMemberValue(
                    target,
                    memberName);
        }

        public static bool TryGetMemberValue(
            object target,
            string memberName,
            out object value,
            out Type valueType)
        {
            return AbeMemberAccessor
                .TryGetMemberValue(
                    target,
                    memberName,
                    out value,
                    out valueType);
        }

        // ================================================================
        // Property Path / Member
        // ================================================================

        public static MemberInfo FindMember(
            Type rootType,
            string propertyPath)
        {
            return AbePropertyPathUtility
                .FindMember(
                    rootType,
                    propertyPath);
        }

        // ================================================================
        // Collection Compatibility
        // ================================================================


        public static object GetCollectionElement(
    object collection,
    int index)
        {
            return AbeCollectionUtility.GetElement(
                collection,
                index);
        }

        // ================================================================
        // Numeric
        // ================================================================

        public static bool TryConvertToFloat(
            object value,
            out float result)
        {
            switch (value)
            {
                case byte v:
                    result = v;
                    return true;

                case sbyte v:
                    result = v;
                    return true;

                case short v:
                    result = v;
                    return true;

                case ushort v:
                    result = v;
                    return true;

                case int v:
                    result = v;
                    return true;

                case uint v:
                    result = v;
                    return true;

                case long v:
                    result = v;
                    return true;

                case ulong v:
                    result = v;
                    return true;

                case float v:
                    result = v;
                    return true;

                case double v:
                    result = (float)v;
                    return true;

                case decimal v:
                    result = (float)v;
                    return true;

                default:
                    result = 0f;
                    return false;
            }
        }
    }
}