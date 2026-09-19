using System;
using System.Reflection;

namespace AbeAttributes.Editor
{
    internal static class AbeMemberAccessor
    {
        // ================================================================
        // Member Type
        // ================================================================

        public static Type GetMemberType(
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

            if (member is MethodInfo method)
            {
                return method.ReturnType;
            }

            return null;
        }

        // ================================================================
        // Get
        // ================================================================

        public static object GetValue(
            object target,
            MemberInfo member)
        {
            if (target == null ||
                member == null)
            {
                return null;
            }

            if (member is FieldInfo field)
            {
                return field.GetValue(
                    target);
            }

            if (member is PropertyInfo property)
            {
                MethodInfo getter =
                    property.GetGetMethod(true);

                return getter?.Invoke(
                    target,
                    null);
            }

            if (member is MethodInfo method)
            {
                if (method.GetParameters().Length != 0)
                {
                    return null;
                }

                return method.Invoke(
                    method.IsStatic
                        ? null
                        : target,
                    null);
            }

            return null;
        }

        // ================================================================
        // Set
        // ================================================================

        public static bool SetValue(
            object target,
            MemberInfo member,
            object value)
        {
            if (target == null ||
                member == null)
            {
                return false;
            }

            if (member is FieldInfo field)
            {
                if (field.IsInitOnly)
                {
                    return false;
                }

                field.SetValue(
                    target,
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
                    target,
                    new[] { value });

                return true;
            }

            return false;
        }

        // ================================================================
        // Member Value
        // ================================================================

        public static object GetMemberValue(
            object target,
            string memberName)
        {
            if (target == null ||
                string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            MemberInfo member =
                AbeMemberScanner
                    .FindFieldOrProperty(
                        target.GetType(),
                        memberName);

            if (member != null)
            {
                return GetValue(
                    target,
                    member);
            }

            MethodInfo method =
                AbeMemberScanner
                    .GetMethod(
                        target.GetType(),
                        memberName);

            if (method != null &&
                method.GetParameters().Length == 0)
            {
                return method.Invoke(
                    method.IsStatic
                        ? null
                        : target,
                    null);
            }

            return null;
        }

        public static bool TryGetMemberValue(
            object target,
            string memberName,
            out object value,
            out Type valueType)
        {
            value = null;
            valueType = null;

            if (target == null ||
                string.IsNullOrEmpty(memberName))
            {
                return false;
            }

            MemberInfo member =
                AbeMemberScanner
                    .FindFieldOrProperty(
                        target.GetType(),
                        memberName);

            if (member != null)
            {
                value =
                    GetValue(
                        target,
                        member);

                valueType =
                    GetMemberType(
                        member);

                return true;
            }

            MethodInfo method =
                AbeMemberScanner
                    .GetMethod(
                        target.GetType(),
                        memberName);

            if (method != null &&
                method.GetParameters().Length == 0)
            {
                value =
                    method.Invoke(
                        method.IsStatic
                            ? null
                            : target,
                        null);

                valueType =
                    method.ReturnType;

                return true;
            }

            return false;
        }
    }
}