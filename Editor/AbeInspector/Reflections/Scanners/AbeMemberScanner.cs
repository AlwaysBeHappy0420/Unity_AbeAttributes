using System;
using System.Collections.Generic;
using System.Reflection;

namespace AbeAttributes.Editor
{
    internal static class AbeMemberScanner
    {
        private static readonly Dictionary<
            Type,
            FieldInfo[]>
            FieldCache =
                new Dictionary<
                    Type,
                    FieldInfo[]>();

        private static readonly Dictionary<
            Type,
            PropertyInfo[]>
            PropertyCache =
                new Dictionary<
                    Type,
                    PropertyInfo[]>();

        private static readonly Dictionary<
            Type,
            MethodInfo[]>
            MethodCache =
                new Dictionary<
                    Type,
                    MethodInfo[]>();

        // ================================================================
        // All Fields
        // ================================================================

        public static IReadOnlyList<FieldInfo>
            GetAllFields(
                Type type)
        {
            if (type == null)
            {
                return Array.Empty<FieldInfo>();
            }

            if (FieldCache.TryGetValue(
                type,
                out FieldInfo[] cached))
            {
                return cached;
            }

            List<FieldInfo> result =
                new List<FieldInfo>();

            List<Type> types =
                GetSelfAndBaseTypes(
                    type);

            // Base -> Derived.
            for (int i = types.Count - 1;
                 i >= 0;
                 i--)
            {
                FieldInfo[] fields =
                    types[i].GetFields(
                        BindingFlags.Instance
                        | BindingFlags.Static
                        | BindingFlags.NonPublic
                        | BindingFlags.Public
                        | BindingFlags.DeclaredOnly);

                result.AddRange(
                    fields);
            }

            FieldInfo[] finalResult =
                result.ToArray();

            FieldCache[type] =
                finalResult;

            return finalResult;
        }

        // ================================================================
        // All Properties
        // ================================================================

        public static IReadOnlyList<PropertyInfo>
            GetAllProperties(
                Type type)
        {
            if (type == null)
            {
                return Array.Empty<PropertyInfo>();
            }

            if (PropertyCache.TryGetValue(
                type,
                out PropertyInfo[] cached))
            {
                return cached;
            }

            List<PropertyInfo> result =
                new List<PropertyInfo>();

            List<Type> types =
                GetSelfAndBaseTypes(
                    type);

            // Base -> Derived.
            for (int i = types.Count - 1;
                 i >= 0;
                 i--)
            {
                PropertyInfo[] properties =
                    types[i].GetProperties(
                        BindingFlags.Instance
                        | BindingFlags.Static
                        | BindingFlags.NonPublic
                        | BindingFlags.Public
                        | BindingFlags.DeclaredOnly);

                result.AddRange(
                    properties);
            }

            PropertyInfo[] finalResult =
                result.ToArray();

            PropertyCache[type] =
                finalResult;

            return finalResult;
        }

        // ================================================================
        // All Methods
        // ================================================================

        public static IReadOnlyList<MethodInfo>
            GetAllMethods(
                Type type)
        {
            if (type == null)
            {
                return Array.Empty<MethodInfo>();
            }

            if (MethodCache.TryGetValue(
                type,
                out MethodInfo[] cached))
            {
                return cached;
            }

            List<MethodInfo> result =
                new List<MethodInfo>();

            List<Type> types =
                GetSelfAndBaseTypes(
                    type);

            // Base -> Derived.
            for (int i = types.Count - 1;
                 i >= 0;
                 i--)
            {
                MethodInfo[] methods =
                    types[i].GetMethods(
                        BindingFlags.Instance
                        | BindingFlags.Static
                        | BindingFlags.NonPublic
                        | BindingFlags.Public
                        | BindingFlags.DeclaredOnly);

                result.AddRange(
                    methods);
            }

            MethodInfo[] finalResult =
                result.ToArray();

            MethodCache[type] =
                finalResult;

            return finalResult;
        }

        // ================================================================
        // Field Lookup
        // ================================================================

        public static FieldInfo FindField(
            Type type,
            string name)
        {
            if (type == null ||
                string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (Type current = type;
                 current != null;
                 current = current.BaseType)
            {
                FieldInfo field =
                    current.GetField(
                        name,
                        BindingFlags.Instance
                        | BindingFlags.Static
                        | BindingFlags.Public
                        | BindingFlags.NonPublic);

                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        // ================================================================
        // Property Lookup
        // ================================================================

        public static PropertyInfo FindProperty(
            Type type,
            string name)
        {
            if (type == null ||
                string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (Type current = type;
                 current != null;
                 current = current.BaseType)
            {
                PropertyInfo property =
                    current.GetProperty(
                        name,
                        BindingFlags.Instance
                        | BindingFlags.Static
                        | BindingFlags.Public
                        | BindingFlags.NonPublic);

                if (property != null)
                {
                    return property;
                }
            }

            return null;
        }

        // ================================================================
        // Method Lookup
        // ================================================================

        public static MethodInfo GetMethod(
            Type type,
            string name)
        {
            if (type == null ||
                string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (Type current = type;
                 current != null;
                 current = current.BaseType)
            {
                MethodInfo method =
                    current.GetMethod(
                        name,
                        BindingFlags.Instance
                        | BindingFlags.Static
                        | BindingFlags.Public
                        | BindingFlags.NonPublic);

                if (method != null)
                {
                    return method;
                }
            }

            return null;
        }

        // ================================================================
        // Field / Property Lookup
        // ================================================================

        public static MemberInfo FindFieldOrProperty(
            Type type,
            string name)
        {
            return FindField(
                       type,
                       name)
                   ?? (MemberInfo)FindProperty(
                       type,
                       name);
        }

        // ================================================================
        // Type Hierarchy
        // ================================================================

        private static List<Type>
            GetSelfAndBaseTypes(
                Type type)
        {
            List<Type> types =
                new List<Type>();

            for (Type current = type;
                 current != null;
                 current = current.BaseType)
            {
                types.Add(
                    current);
            }

            return types;
        }
    }
}