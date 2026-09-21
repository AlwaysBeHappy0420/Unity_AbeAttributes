using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AbeAttributes
{
    public static class PopToConsoleRuntime
    {
        public static void OnInvoke(
            string typeName,
            string methodName)
        {
            Debug.Log(
                "[PopToConsole] Invoke: " +
                typeName + "." +
                methodName);
        }

        public static void OnGet(
            string typeName,
            string propertyName,
            object value)
        {
            Debug.Log(
                "[PopToConsole] Get: " +
                typeName + "." +
                propertyName +
                " = " +
                value);
        }

        public static void OnGet(
            string typeName,
            string propertyName,
            object value,
            string valuePath)
        {
            if (string.IsNullOrEmpty(valuePath))
            {
                OnGet(
                    typeName,
                    propertyName,
                    value);

                return;
            }

            if (!TryGetValuePath(
                    value,
                    valuePath,
                    out object selectedValue))
            {
                Debug.LogWarning(
                    "[PopToConsole] ValuePath not found: " +
                    typeName + "." +
                    propertyName + "." +
                    valuePath);

                OnGet(
                    typeName,
                    propertyName,
                    value);

                return;
            }

            Debug.Log(
                "[PopToConsole] Get: " +
                typeName + "." +
                propertyName + "." +
                valuePath +
                " = " +
                selectedValue);
        }

        public static void OnSet(
            string typeName,
            string propertyName,
            object value)
        {
            Debug.Log(
                "[PopToConsole] Set: " +
                typeName + "." +
                propertyName +
                " = " +
                value);
        }

        public static void OnSet(
            string typeName,
            string propertyName,
            object value,
            string valuePath)
        {
            if (string.IsNullOrEmpty(valuePath))
            {
                OnSet(
                    typeName,
                    propertyName,
                    value);

                return;
            }

            if (!TryGetValuePath(
                    value,
                    valuePath,
                    out object selectedValue))
            {
                Debug.LogWarning(
                    "[PopToConsole] ValuePath not found: " +
                    typeName + "." +
                    propertyName + "." +
                    valuePath);

                OnSet(
                    typeName,
                    propertyName,
                    value);

                return;
            }

            Debug.Log(
                "[PopToConsole] Set: " +
                typeName + "." +
                propertyName + "." +
                valuePath +
                " = " +
                selectedValue);
        }

        public static void OnManual(
            string typeName,
            string memberName)
        {
            Debug.Log(
                "[PopToConsole] Manual: " +
                typeName + "." +
                memberName);
        }

        public static void OnManual(
            string typeName,
            string memberName,
            object value)
        {
            Debug.Log(
                "[PopToConsole] Manual: " +
                typeName + "." +
                memberName +
                " = " +
                value);
        }

        public static void Break()
        {
            Debug.Log(
                "Break Point Reached");

            Debug.Break();
        }

        private static bool TryGetValuePath(
            object root,
            string valuePath,
            out object result)
        {
            result = null;

            if (root == null ||
                string.IsNullOrEmpty(valuePath))
            {
                return false;
            }

            object current = root;
            string[] parts =
                valuePath.Split('.');

            for (int i = 0;
                 i < parts.Length;
                 i++)
            {
                if (current == null)
                {
                    return false;
                }

                string part = parts[i];

                if (part == "Array" &&
                    i + 1 < parts.Length &&
                    TryParseDataIndex(
                        parts[i + 1],
                        out int index))
                {
                    if (!TryGetCollectionElement(
                            current,
                            index,
                            out object element))
                    {
                        return false;
                    }

                    current = element;
                    i++;
                    continue;
                }

                MemberInfo member =
                    FindMember(
                        current.GetType(),
                        part);

                if (member == null)
                {
                    return false;
                }

                if (member is FieldInfo field)
                {
                    current =
                        field.GetValue(current);

                    continue;
                }

                if (member is PropertyInfo property)
                {
                    if (property.GetIndexParameters().Length > 0)
                    {
                        return false;
                    }

                    MethodInfo getter =
                        property.GetGetMethod(true);

                    if (getter == null)
                    {
                        return false;
                    }

                    current =
                        getter.Invoke(
                            getter.IsStatic
                                ? null
                                : current,
                            null);

                    continue;
                }

                return false;
            }

            result = current;
            return true;
        }

        private static MemberInfo FindMember(
            Type type,
            string name)
        {
            if (type == null ||
                string.IsNullOrEmpty(name))
            {
                return null;
            }

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            Type current = type;

            while (current != null)
            {
                FieldInfo field =
                    current.GetField(
                        name,
                        flags);

                if (field != null)
                {
                    return field;
                }

                PropertyInfo property =
                    current.GetProperty(
                        name,
                        flags);

                if (property != null)
                {
                    return property;
                }

                current = current.BaseType;
            }

            return null;
        }

        private static bool TryGetCollectionElement(
            object collection,
            int index,
            out object value)
        {
            value = null;

            if (collection == null ||
                index < 0)
            {
                return false;
            }

            if (collection is Array array)
            {
                if (index >= array.Length)
                {
                    return false;
                }

                value =
                    array.GetValue(index);

                return true;
            }

            if (collection is IList list)
            {
                if (index >= list.Count)
                {
                    return false;
                }

                value = list[index];
                return true;
            }

            return false;
        }

        private static bool TryParseDataIndex(
            string value,
            out int index)
        {
            index = -1;

            if (string.IsNullOrEmpty(value) ||
                !value.StartsWith("data[") ||
                !value.EndsWith("]"))
            {
                return false;
            }

            string number =
                value.Substring(
                    5,
                    value.Length - 6);

            return int.TryParse(
                number,
                out index);
        }
    }
}
