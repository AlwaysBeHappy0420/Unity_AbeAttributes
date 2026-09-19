using System;
using System.Reflection;

namespace AbeAttributes.Editor
{
    public sealed class InspectorPropertyInfo
    {
        public MemberInfo MemberInfo { get; }

        public Type ValueType { get; }

        public string Name { get; }

        public bool IsField =>
            MemberInfo is FieldInfo;

        public bool IsProperty =>
            MemberInfo is PropertyInfo;

        public bool IsMethod =>
            MemberInfo is MethodInfo;

        public FieldInfo FieldInfo =>
            MemberInfo as FieldInfo;

        public PropertyInfo PropertyInfo =>
            MemberInfo as PropertyInfo;

        public MethodInfo MethodInfo =>
            MemberInfo as MethodInfo;

        public InspectorPropertyInfo(
            MemberInfo memberInfo,
            Type fallbackValueType = null,
            string fallbackName = null)
        {
            MemberInfo = memberInfo;
            Name =
                memberInfo?.Name
                ?? fallbackName
                ?? string.Empty;

            ValueType =
                GetValueType(memberInfo)
                ?? fallbackValueType;
        }

        private static Type GetValueType(
            MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo field)
            {
                return field.FieldType;
            }

            if (memberInfo is PropertyInfo property)
            {
                return property.PropertyType;
            }

            if (memberInfo is MethodInfo method)
            {
                return method.ReturnType;
            }

            return null;
        }
    }
}
