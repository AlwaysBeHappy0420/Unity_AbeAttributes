using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace AbeAttributes.Editor
{
    public sealed class AbeValueEntry
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