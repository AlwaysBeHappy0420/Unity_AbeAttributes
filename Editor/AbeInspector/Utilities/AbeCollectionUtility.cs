using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace AbeAttributes.Editor
{
    internal static class AbeCollectionUtility
    {
        // ====================================================================
        // Collection Type
        // ====================================================================

        public static bool IsCollectionType(Type type)
        {
            if (type == null || type == typeof(string))
                return false;

            // ------------------------------------------------------------
            // One-dimensional Array
            // ------------------------------------------------------------

            if (type.IsArray)
                return type.GetArrayRank() == 1;

            // ------------------------------------------------------------
            // Non-generic IList
            // ------------------------------------------------------------

            if (typeof(IList).IsAssignableFrom(type))
                return true;

            // ------------------------------------------------------------
            // Generic IList<T>
            // ------------------------------------------------------------

            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(IList<>))
            {
                return true;
            }

            // ------------------------------------------------------------
            // Types implementing IList<T>
            // ------------------------------------------------------------

            Type[] interfaces =
                type.GetInterfaces();

            for (int i = 0;
                 i < interfaces.Length;
                 i++)
            {
                Type current =
                    interfaces[i];

                if (!current.IsGenericType)
                    continue;

                if (current.GetGenericTypeDefinition() ==
                    typeof(IList<>))
                {
                    return true;
                }
            }

            return false;
        }

        // ====================================================================
        // Element Type
        // ====================================================================

        public static Type GetElementType(Type collectionType)
        {
            if (collectionType == null)
                return null;

            if (collectionType.IsArray)
            {
                if (collectionType.GetArrayRank() != 1)
                    return null;

                return collectionType.GetElementType();
            }

            if (collectionType.IsGenericType &&
                collectionType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return collectionType.GetGenericArguments()[0];
            }

            Type[] interfaces =
                collectionType.GetInterfaces();

            for (int i = 0;
                 i < interfaces.Length;
                 i++)
            {
                Type current =
                    interfaces[i];

                if (current.IsGenericType &&
                    current.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    return current.GetGenericArguments()[0];
                }
            }

            return null;
        }

        // ====================================================================
        // Serialized Collection
        // ====================================================================

        public static bool IsSerializedCollection(
            SerializedProperty property)
        {
            return property != null &&
                   property.isArray &&
                   property.propertyType !=
                   SerializedPropertyType.String;
        }

        // ====================================================================
        // Count
        // ====================================================================

        public static int GetCount(
            object collection)
        {
            if (collection == null)
                return 0;

            if (collection is Array array)
            {
                if (array.Rank != 1)
                    return 0;

                return array.Length;
            }

            if (collection is IList list)
            {
                return list.Count;
            }

            return 0;
        }

        // ====================================================================
        // Resizable
        // ====================================================================

        public static bool IsResizable(
            object collection)
        {
            if (collection == null)
                return false;

            if (collection is Array array)
            {
                return array.Rank == 1;
            }

            if (collection is IList list)
            {
                return
                    !list.IsReadOnly &&
                    !list.IsFixedSize;
            }

            return false;
        }

        // ====================================================================
        // Can Write Element
        // ====================================================================

        public static bool CanWriteElement(
            object collection,
            int index)
        {
            if (collection == null ||
                index < 0)
            {
                return false;
            }

            if (collection is Array array)
            {
                if (array.Rank != 1)
                    return false;

                return index < array.Length;
            }

            if (collection is IList list)
            {
                return
                    index < list.Count &&
                    !list.IsReadOnly &&
                    !list.IsFixedSize;
            }

            return false;
        }

        // ====================================================================
        // Get Element
        // ====================================================================

        public static object GetElement(
            object collection,
            int index)
        {
            if (collection == null ||
                index < 0)
            {
                return null;
            }

            if (collection is Array array)
            {
                // Multi-dimensional arrays are not supported.
                if (array.Rank != 1)
                    return null;

                if (index >= array.Length)
                    return null;

                return array.GetValue(index);
            }

            if (collection is IList list)
            {
                if (index >= list.Count)
                    return null;

                return list[index];
            }

            if (collection is IEnumerable enumerable)
            {
                int currentIndex = 0;

                foreach (object element in enumerable)
                {
                    if (currentIndex == index)
                        return element;

                    currentIndex++;
                }
            }

            return null;
        }

        // ====================================================================
        // Set Element
        // ====================================================================

        public static bool SetElement(
            object collection,
            int index,
            object value)
        {
            if (collection == null ||
                index < 0)
            {
                return false;
            }

            if (collection is Array array)
            {
                if (array.Rank != 1)
                    return false;

                if (index >= array.Length)
                    return false;

                array.SetValue(
                    value,
                    index);

                return true;
            }

            if (collection is IList list)
            {
                if (index >= list.Count ||
                    list.IsReadOnly ||
                    list.IsFixedSize)
                {
                    return false;
                }

                list[index] = value;
                return true;
            }

            return false;
        }

        // ====================================================================
        // Default Element
        // ====================================================================

        public static object CreateDefaultElement(
            Type type)
        {
            if (type == null)
                return null;

            if (type == typeof(string))
                return string.Empty;

            if (type == typeof(UnityEngine.Object) ||
                type.IsSubclassOf(
                    typeof(UnityEngine.Object)))
            {
                return null;
            }

            if (type.IsInterface ||
                type.IsAbstract)
            {
                return null;
            }

            if (type.IsValueType)
                return Activator.CreateInstance(type);

            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }
    }
}