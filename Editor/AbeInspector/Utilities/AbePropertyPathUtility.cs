using System;
using System.Reflection;

namespace AbeAttributes.Editor
{
    internal static class AbePropertyPathUtility
    {
        // ====================================================================
        // Get Value
        // ====================================================================

        public static object GetValue(
            object target,
            string propertyPath)
        {
            if (target == null ||
                string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            object current = target;
            Type currentType = target.GetType();

            string[] parts =
                propertyPath.Split('.');

            for (int i = 0;
                 i < parts.Length;
                 i++)
            {
                if (current == null)
                {
                    return null;
                }

                string part =
                    parts[i];

                // ------------------------------------------------------------
                // Array.data[index]
                // ------------------------------------------------------------

                if (part == "Array" &&
                    i + 1 < parts.Length &&
                    TryParseDataIndex(
                        parts[i + 1],
                        out int index))
                {
                    current =
                        AbeCollectionUtility.GetElement(
                            current,
                            index);

                    if (current == null)
                    {
                        return null;
                    }

                    currentType =
                        current.GetType();

                    i++;
                    continue;
                }

                // ------------------------------------------------------------
                // Normal member
                // ------------------------------------------------------------

                MemberInfo member =
                    FindDirectMember(
                        currentType,
                        part);

                if (member == null)
                {
                    return null;
                }

                current =
                    GetMemberValue(
                        current,
                        member);

                if (current == null)
                {
                    return null;
                }

                currentType =
                    current.GetType();
            }

            return current;
        }

        // ====================================================================
        // Find Member
        //
        // Supports:
        //
        //   "field"
        //   "foo.bar"
        //   "list.Array.data[0].bar"
        //
        // ====================================================================

        internal static MemberInfo FindMember(
            Type type,
            string propertyPath)
        {
            if (type == null ||
                string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            string[] parts =
                propertyPath.Split('.');

            Type currentType =
                type;

            for (int i = 0;
                 i < parts.Length;
                 i++)
            {
                if (currentType == null)
                {
                    return null;
                }

                string part =
                    parts[i];

                // ------------------------------------------------------------
                // Array.data[index]
                //
                // The actual element value is not available here,
                // so resolve its declared element type.
                // ------------------------------------------------------------

                if (part == "Array" &&
                    i + 1 < parts.Length &&
                    TryParseDataIndex(
                        parts[i + 1],
                        out _))
                {
                    currentType =
                        AbeCollectionUtility.GetElementType(
                            currentType);

                    if (currentType == null)
                    {
                        return null;
                    }

                    i++;
                    continue;
                }

                // ------------------------------------------------------------
                // Normal member
                // ------------------------------------------------------------

                MemberInfo member =
                    FindDirectMember(
                        currentType,
                        part);

                if (member == null)
                {
                    return null;
                }

                // ------------------------------------------------------------
                // This is the requested member.
                // ------------------------------------------------------------

                if (i == parts.Length - 1)
                {
                    return member;
                }

                currentType =
                    GetMemberType(member);
            }

            return null;
        }

        // ====================================================================
        // Direct Member Lookup
        // ====================================================================

        private static MemberInfo FindDirectMember(
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

            Type currentType =
                type;

            while (currentType != null)
            {
                FieldInfo field =
                    currentType.GetField(
                        name,
                        flags);

                if (field != null)
                {
                    return field;
                }

                PropertyInfo property =
                    currentType.GetProperty(
                        name,
                        flags);

                if (property != null)
                {
                    return property;
                }

                currentType =
                    currentType.BaseType;
            }

            return null;
        }

        // ====================================================================
        // Get Member Value
        // ====================================================================

        private static object GetMemberValue(
            object owner,
            MemberInfo member)
        {
            if (owner == null ||
                member == null)
            {
                return null;
            }

            if (member is FieldInfo field)
            {
                return field.GetValue(
                    owner);
            }

            if (member is PropertyInfo property)
            {
                MethodInfo getter =
                    property.GetGetMethod(true);

                if (getter == null)
                {
                    return null;
                }

                return getter.Invoke(
                    owner,
                    null);
            }

            return null;
        }

        // ====================================================================
        // Get Member Type
        // ====================================================================

        private static Type GetMemberType(
            MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return field.FieldType;
            }

            if (member is PropertyInfo property)
            {
                return property.PropertyType;
            }

            return null;
        }

        // ====================================================================
        // Parse data[index]
        // ====================================================================

        private static bool TryParseDataIndex(
            string value,
            out int index)
        {
            index = -1;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (!value.StartsWith("data[") ||
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

        // ====================================================================
        // Set Value
        // ====================================================================

        public static bool SetValue(
            object target,
            string propertyPath,
            object value)
        {
            if (target == null ||
                string.IsNullOrEmpty(propertyPath))
            {
                return false;
            }

            string[] parts =
                propertyPath.Split('.');

            return SetValueRecursive(
                target,
                target.GetType(),
                parts,
                0,
                value,
                out _);
        }

        // ====================================================================
        // Set Value Recursive
        // ====================================================================

        private static bool SetValueRecursive(
            object owner,
            Type ownerType,
            string[] parts,
            int index,
            object value,
            out object updatedOwner)
        {
            updatedOwner = owner;

            if (owner == null ||
                ownerType == null ||
                index >= parts.Length)
            {
                return false;
            }

            string part =
                parts[index];

            // ------------------------------------------------------------
            // Array.data[index]
            // ------------------------------------------------------------

            if (part == "Array" &&
                index + 1 < parts.Length &&
                TryParseDataIndex(
                    parts[index + 1],
                    out int elementIndex))
            {
                object element =
                    AbeCollectionUtility.GetElement(
                        owner,
                        elementIndex);

                if (element == null)
                {
                    return false;
                }

                Type elementType =
                    element.GetType();

                if (index + 2 >= parts.Length)
                {
                    return AbeCollectionUtility.SetElement(
                        owner,
                        elementIndex,
                        value);
                }

                bool success =
                    SetValueRecursive(
                        element,
                        elementType,
                        parts,
                        index + 2,
                        value,
                        out object updatedElement);

                if (!success)
                {
                    return false;
                }

                if (elementType.IsValueType)
                {
                    return AbeCollectionUtility.SetElement(
                        owner,
                        elementIndex,
                        updatedElement);
                }

                updatedOwner =
                    owner;

                return true;
            }

            // ------------------------------------------------------------
            // Normal member
            // ------------------------------------------------------------

            MemberInfo member =
                FindDirectMember(
                    ownerType,
                    part);

            if (member == null)
            {
                return false;
            }

            Type memberType =
                GetMemberType(member);

            // ------------------------------------------------------------
            // Leaf
            // ------------------------------------------------------------

            if (index == parts.Length - 1)
            {
                return SetMemberValue(
                    owner,
                    member,
                    value);
            }

            // ------------------------------------------------------------
            // Nested member
            // ------------------------------------------------------------

            object child =
                GetMemberValue(
                    owner,
                    member);

            if (child == null)
            {
                return false;
            }

            bool childSuccess =
                SetValueRecursive(
                    child,
                    memberType,
                    parts,
                    index + 1,
                    value,
                    out object updatedChild);

            if (!childSuccess)
            {
                return false;
            }

            // ------------------------------------------------------------
            // Struct / value type
            // ------------------------------------------------------------

            if (memberType.IsValueType)
            {
                return SetMemberValue(
                    owner,
                    member,
                    updatedChild);
            }

            updatedOwner =
                owner;

            return true;
        }

        // ====================================================================
        // Set Member Value
        // ====================================================================

        private static bool SetMemberValue(
            object owner,
            MemberInfo member,
            object value)
        {
            if (owner == null ||
                member == null)
            {
                return false;
            }

            if (member is FieldInfo field)
            {
                if (field.IsInitOnly ||
                    field.IsLiteral)
                {
                    return false;
                }

                field.SetValue(
                    owner,
                    value);

                return true;
            }

            if (member is PropertyInfo property)
            {
                MethodInfo setter =
                    property.GetSetMethod(true);

                if (setter == null)
                {
                    return false;
                }

                setter.Invoke(
                    owner,
                    new[] { value });

                return true;
            }

            return false;
        }

        // ====================================================================
        // Get Parent Object
        // ====================================================================

        public static object GetParentObject(
            object target,
            string propertyPath)
        {
            if (target == null ||
                string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            string[] parts =
                propertyPath.Split('.');

            if (parts.Length <= 1)
            {
                return target;
            }

            object current =
                target;

            Type currentType =
                target.GetType();

            for (int i = 0;
                 i < parts.Length - 1;
                 i++)
            {
                if (current == null)
                {
                    return null;
                }

                string part =
                    parts[i];

                // ------------------------------------------------------------
                // Array.data[index]
                // ------------------------------------------------------------

                if (part == "Array" &&
                    i + 1 < parts.Length &&
                    TryParseDataIndex(
                        parts[i + 1],
                        out int elementIndex))
                {
                    current =
                        AbeCollectionUtility.GetElement(
                            current,
                            elementIndex);

                    if (current == null)
                    {
                        return null;
                    }

                    currentType =
                        current.GetType();

                    i++;
                    continue;
                }

                // ------------------------------------------------------------
                // Normal member
                // ------------------------------------------------------------

                MemberInfo member =
                    FindDirectMember(
                        currentType,
                        part);

                if (member == null)
                {
                    return null;
                }

                current =
                    GetMemberValue(
                        current,
                        member);

                if (current == null)
                {
                    return null;
                }

                currentType =
                    current.GetType();
            }

            return current;
        }
    }
}