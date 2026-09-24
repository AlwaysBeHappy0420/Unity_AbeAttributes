using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace AbeAttributes.Editor
{
    internal sealed class AbeValueEntry
    {
        private readonly AbeProperty _property;

        private readonly AbeSerializedValueAccessor
            _serializedAccessor;

        private readonly AbeCollectionAccessor
            _collectionAccessor;

        private readonly AbeReflectionValueAccessor
            _reflectionAccessor;

        private readonly AbeMethodInvoker
            _methodInvoker;

        internal AbeSerializedValueAccessor SerializedAccessor =>
            _serializedAccessor;

        internal AbeCollectionAccessor CollectionAccessor =>
            _collectionAccessor;

        public Type ValueType { get; }

        public IReadOnlyList<UnityEngine.Object> Targets =>
            _property.Tree.Targets;

        internal IReadOnlyList<object> LastMethodResults =>
            _methodInvoker.LastResults;

        internal bool HasInvokedMethod =>
            _methodInvoker.HasInvoked;

        public bool HasMultipleDifferentValues
        {
            get
            {
                if (_property.Kind ==
                    AbePropertyKind.Method)
                {
                    return false;
                }

                SerializedProperty serializedProperty =
                    _property.SerializedProperty;

                if (serializedProperty != null)
                {
                    return serializedProperty
                        .hasMultipleDifferentValues;
                }

                object[] values =
                    GetValues();

                if (values.Length <= 1)
                {
                    return false;
                }

                object first =
                    values[0];

                for (int i = 1;
                     i < values.Length;
                     i++)
                {
                    if (!ValuesEqual(
                            first,
                            values[i]))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool CanWrite
        {
            get
            {
                if (_property.Kind ==
                    AbePropertyKind.Method)
                {
                    return false;
                }

                if (_property.SerializedProperty != null)
                {
                    return true;
                }

                if (_property.IsNativeCollectionElement)
                {
                    return _collectionAccessor
                        .CanWriteElement();
                }

                MemberInfo member =
                    _property.Info?.MemberInfo;

                if (member is FieldInfo field)
                {
                    return !field.IsInitOnly;
                }

                if (member is PropertyInfo property)
                {
                    return property.GetSetMethod(true)
                           != null;
                }

                return false;
            }
        }

        public AbeValueEntry(
            AbeProperty property,
            Type valueType)
        {
            _property =
                property;

            ValueType =
                valueType
                ?? typeof(object);

            // Reflection must be created first because Collection and
            // Method use callbacks into it.
            _reflectionAccessor =
                new AbeReflectionValueAccessor(
                    this,
                    _property);

            _serializedAccessor =
                new AbeSerializedValueAccessor(
                    this,
                    _property);

            _collectionAccessor =
                new AbeCollectionAccessor(
                    this,
                    _property,
                    _reflectionAccessor
                        .SetPropertyChainValueForTarget,
                    _reflectionAccessor
                        .CanWriteCollectionMember);

            _methodInvoker =
                new AbeMethodInvoker(
                    _property,
                    Targets,
                    _reflectionAccessor
                        .ResolveOwnerForTarget);
        }

        // ================================================================
        // Get
        // ================================================================

        public object GetValue()
        {
            if (_property.Kind ==
                AbePropertyKind.Method)
            {
                return null;
            }

            object[] values =
                GetValues();

            if (values.Length == 0)
            {
                return null;
            }

            return values[0];
        }

        public object[] GetValues()
        {
            if (_property.Kind ==
                AbePropertyKind.Method)
            {
                return Array.Empty<object>();
            }

            IReadOnlyList<UnityEngine.Object> targets =
                Targets;

            if (targets == null ||
                targets.Count == 0)
            {
                return Array.Empty<object>();
            }

            object[] values =
                new object[targets.Count];

            for (int i = 0;
                 i < targets.Count;
                 i++)
            {
                values[i] =
                    GetValueForTarget(
                        targets[i]);
            }

            return values;
        }

        public bool TryGetValue<T>(
            out T value)
        {
            object current =
                GetValue();

            if (current is T typed)
            {
                value =
                    typed;

                return true;
            }

            value =
                default;

            return false;
        }

        // ================================================================
        // Method
        // ================================================================

        internal object[] GetMethodParameterValues()
        {
            return _methodInvoker
                .GetParameterValues();
        }

        internal void SetMethodParameterValue(
            int index,
            object value)
        {
            _methodInvoker
                .SetParameterValue(
                    index,
                    value);
        }

        public bool CanInvokeMethod()
        {
            return _methodInvoker.CanInvoke();
        }

        public object[] InvokeMethod()
        {
            return _methodInvoker.Invoke();
        }

        public object[] InvokeMethod(
            object[] arguments)
        {
            return _methodInvoker.Invoke(
                arguments);
        }

        // ================================================================
        // Get For Target
        // ================================================================

        public object GetValueForTarget(
            UnityEngine.Object target)
        {
            if (target == null)
            {
                return null;
            }

            if (_property.Kind ==
                AbePropertyKind.Method)
            {
                return null;
            }

            if (_property.SerializedProperty != null)
            {
                return _serializedAccessor
                    .GetValueForTarget(
                        target);
            }

            if (_property.IsNativeCollectionElement)
            {
                return _collectionAccessor
                    .GetElementForTarget(
                        target,
                        _property.CollectionIndex);
            }

            return _reflectionAccessor
                .GetValueForTarget(
                    target);
        }

        // ================================================================
        // Set
        // ================================================================

        public void SetValue(
            object value)
        {
            if (_property.Kind ==
                AbePropertyKind.Method)
            {
                return;
            }

            value =
                PrepareValueForTargetType(
                    value);

            if (_property.SerializedProperty != null)
            {
                _serializedAccessor
                    .SetValue(
                        value);

                return;
            }

            _reflectionAccessor
                .SetValue(
                    value);
        }

        // ================================================================
        // Value Conversion
        // ================================================================

        private object PrepareValueForTargetType(
            object value)
        {
            Type targetType =
                ValueType;

            if (targetType == null ||
                targetType == typeof(object))
            {
                return value;
            }

            // Already the correct type.
            if (value != null &&
                targetType.IsInstanceOfType(value))
            {
                return value;
            }

            // ------------------------------------------------------------
            // Null
            // ------------------------------------------------------------

            if (value == null)
            {
                if (IsListType(targetType))
                {
                    Type elementType =
                        targetType.GetGenericArguments()[0];

                    if (elementType.IsValueType &&
                        Nullable.GetUnderlyingType(
                            elementType) == null)
                    {
                        throw new ArgumentException(
                            $"Cannot add null to " +
                            $"'{targetType}'.");
                    }

                    return CreateSingleElementList(
                        targetType,
                        null);
                }

                return null;
            }

            // ------------------------------------------------------------
            // List<T>
            //
            // A single selected Dropdown value is wrapped
            // into a List<T>.
            //
            // T       -> T
            // List<T> -> List<T> { value }
            // ------------------------------------------------------------

            if (IsListType(targetType))
            {
                return CreateSingleElementList(
                    targetType,
                    value);
            }

            // ------------------------------------------------------------
            // Non-list target
            // ------------------------------------------------------------

            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            return ConvertValueForType(
                value,
                targetType);
        }

        private static bool IsListType(
            Type type)
        {
            return
                type != null &&
                type.IsGenericType &&
                type.GetGenericTypeDefinition() ==
                typeof(List<>);
        }

        private static object CreateSingleElementList(
            Type listType,
            object value)
        {
            Type elementType =
                listType.GetGenericArguments()[0];

            object convertedValue =
                ConvertValueForType(
                    value,
                    elementType);

            IList list =
                (IList)Activator.CreateInstance(
                    listType);

            list.Add(
                convertedValue);

            return list;
        }

        private static object ConvertValueForType(
            object value,
            Type targetType)
        {
            if (targetType == null)
            {
                return value;
            }

            if (value == null)
            {
                if (targetType.IsValueType &&
                    Nullable.GetUnderlyingType(
                        targetType) == null)
                {
                    throw new ArgumentException(
                        $"Cannot assign null to " +
                        $"value type '{targetType}'.");
                }

                return null;
            }

            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            // ------------------------------------------------------------
            // Nullable<T>
            // ------------------------------------------------------------

            Type nullableType =
                Nullable.GetUnderlyingType(
                    targetType);

            if (nullableType != null)
            {
                object converted =
                    ConvertValueForType(
                        value,
                        nullableType);

                return Activator.CreateInstance(
                    targetType,
                    converted);
            }

            // ------------------------------------------------------------
            // Enum
            // ------------------------------------------------------------

            if (targetType.IsEnum)
            {
                if (value is string text)
                {
                    return Enum.Parse(
                        targetType,
                        text);
                }

                return Enum.ToObject(
                    targetType,
                    value);
            }

            // ------------------------------------------------------------
            // Standard convertible values
            // ------------------------------------------------------------

            try
            {
                return Convert.ChangeType(
                    value,
                    targetType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Value of type " +
                    $"'{value.GetType()}' cannot be " +
                    $"converted to '{targetType}'.",
                    ex);
            }
        }

        // ================================================================
        // Collection
        // ================================================================

        public int GetCollectionCount()
        {
            return _collectionAccessor
                .GetCount();
        }

        public int GetCollectionCount(
            out bool hasDifferentSizes)
        {
            return _collectionAccessor
                .GetCount(
                    out hasDifferentSizes);
        }

        public bool CanResizeCollection()
        {
            return _collectionAccessor
                .CanResize();
        }

        public bool TrySetCollectionSize(
            int newSize)
        {
            return _collectionAccessor
                .TrySetSize(
                    newSize);
        }

        // ================================================================
        // Equality
        // ================================================================

        private static bool ValuesEqual(
            object a,
            object b)
        {
            if (ReferenceEquals(
                    a,
                    b))
            {
                return true;
            }

            if (a == null ||
                b == null)
            {
                return false;
            }

            return a.Equals(b);
        }
    }
}