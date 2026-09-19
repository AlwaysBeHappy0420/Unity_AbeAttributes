using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace AbeAttributes.Editor
{
    internal sealed class AbeReflectionValueAccessor
    {
        private readonly AbeValueEntry _entry;
        private readonly AbeProperty _property;

        public AbeReflectionValueAccessor(
            AbeValueEntry entry,
            AbeProperty property)
        {
            _entry =
                entry;

            _property =
                property;
        }

        // ================================================================
        // Get
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

            MemberInfo member =
                _property.Info?.MemberInfo;

            if (member == null)
            {
                return null;
            }

            object owner =
                ResolveOwnerForTarget(
                    target);

            if (owner == null)
            {
                return null;
            }

            return AbeReflectionUtility.GetValue(
                owner,
                member);
        }

        // ================================================================
        // Set
        // ================================================================

        public void SetValue(
            object value)
        {
            if (_property == null)
            {
                return;
            }

            UnityEngine.Object[] targets =
                GetUnityTargets();

            if (targets.Length == 0)
            {
                return;
            }

            Undo.RecordObjects(
                targets,
                "Change "
                + _property.Name);

            for (int i = 0;
                 i < targets.Length;
                 i++)
            {
                UnityEngine.Object target =
                    targets[i];

                if (target == null)
                {
                    continue;
                }

                bool changed =
                    SetPropertyChainValueForTarget(
                        target,
                        value);

                if (!changed)
                {
                    continue;
                }

                EditorUtility.SetDirty(
                    target);
            }
        }

        // ================================================================
        // Property Chain
        // ================================================================

        internal bool SetPropertyChainValueForTarget(
            UnityEngine.Object target,
            object value)
        {
            if (target == null)
            {
                return false;
            }

            List<AbeProperty> chain =
                BuildPropertyChain();

            if (chain.Count == 0)
            {
                return false;
            }

            return SetPropertyChainRecursive(
                target,
                target,
                chain,
                0,
                value);
        }

        private List<AbeProperty>
            BuildPropertyChain()
        {
            List<AbeProperty> chain =
                new List<AbeProperty>();

            AbeProperty current =
                _property;

            while (current != null)
            {
                chain.Add(
                    current);

                current =
                    current.Parent;
            }

            chain.Reverse();

            return chain;
        }

        private bool SetPropertyChainRecursive(
            object current,
            UnityEngine.Object rootTarget,
            List<AbeProperty> chain,
            int index,
            object value)
        {
            if (current == null ||
                chain == null ||
                index >= chain.Count)
            {
                return false;
            }

            AbeProperty property =
                chain[index];

            if (property == null)
            {
                return false;
            }

            // ------------------------------------------------------------
            // Group
            // ------------------------------------------------------------

            if (property.IsGroup)
            {
                return SetPropertyChainRecursive(
                    current,
                    rootTarget,
                    chain,
                    index + 1,
                    value);
            }

            // ------------------------------------------------------------
            // Serialized boundary
            // ------------------------------------------------------------

            if (property.SerializedProperty != null)
            {
                if (!(current
                    is UnityEngine.Object unityObject))
                {
                    return false;
                }

                object serializedValue =
                    property.ValueEntry
                        .GetValueForTarget(
                            unityObject);

                if (index == chain.Count - 1)
                {
                    return _entry
                        .SerializedAccessor
                        .SetValueForTarget(
                            unityObject,
                            property.SerializedProperty
                                .propertyPath,
                            value);
                }

                if (serializedValue == null)
                {
                    return false;
                }

                bool changed =
                    SetNestedValueRecursive(
                        serializedValue,
                        chain,
                        index + 1,
                        value);

                if (!changed)
                {
                    return false;
                }

                Type serializedType =
                    serializedValue.GetType();

                if (!serializedType.IsValueType)
                {
                    return true;
                }

                return AbeReflectionUtility.SetValue(
                    unityObject,
                    property.SerializedProperty
                        .propertyPath,
                    serializedValue);
            }

            return SetNestedValueRecursive(
                current,
                chain,
                index,
                value);
        }

        private bool SetNestedValueRecursive(
            object current,
            List<AbeProperty> chain,
            int index,
            object value)
        {
            if (current == null ||
                chain == null ||
                index >= chain.Count)
            {
                return false;
            }

            AbeProperty property =
                chain[index];

            if (property == null)
            {
                return false;
            }

            // ------------------------------------------------------------
            // Group
            // ------------------------------------------------------------

            if (property.IsGroup)
            {
                return SetNestedValueRecursive(
                    current,
                    chain,
                    index + 1,
                    value);
            }

            // ------------------------------------------------------------
            // Serialized
            // ------------------------------------------------------------

            if (property.SerializedProperty != null)
            {
                return false;
            }

            // ------------------------------------------------------------
            // Native Collection Element
            // ------------------------------------------------------------

            if (property.IsNativeCollectionElement)
            {
                object element =
                    AbeReflectionUtility
                        .GetCollectionElement(
                            current,
                            property.CollectionIndex);

                if (index == chain.Count - 1)
                {
                    return _entry
                        .CollectionAccessor
                        .SetElement(
                            current,
                            property.CollectionIndex,
                            value);
                }

                if (element == null)
                {
                    return false;
                }

                bool changed =
                    SetNestedValueRecursive(
                        element,
                        chain,
                        index + 1,
                        value);

                if (!changed)
                {
                    return false;
                }

                if (!element.GetType().IsValueType)
                {
                    return true;
                }

                return _entry
                    .CollectionAccessor
                    .SetElement(
                        current,
                        property.CollectionIndex,
                        element);
            }

            // ------------------------------------------------------------
            // Reflection Member
            // ------------------------------------------------------------

            MemberInfo member =
                property.Info?.MemberInfo;

            if (member == null)
            {
                return false;
            }

            if (index == chain.Count - 1)
            {
                return AbeReflectionUtility.SetValue(
                    current,
                    member,
                    value);
            }

            object child =
                AbeReflectionUtility.GetValue(
                    current,
                    member);

            if (child == null)
            {
                return false;
            }

            bool childChanged =
                SetNestedValueRecursive(
                    child,
                    chain,
                    index + 1,
                    value);

            if (!childChanged)
            {
                return false;
            }

            if (!child.GetType().IsValueType)
            {
                return true;
            }

            return AbeReflectionUtility.SetValue(
                current,
                member,
                child);
        }

        // ================================================================
        // Collection Resize Support
        // ================================================================

        internal bool CanWriteCollectionMember()
        {
            List<AbeProperty> chain =
                BuildPropertyChain();

            for (int i = chain.Count - 1;
                 i >= 0;
                 i--)
            {
                AbeProperty property =
                    chain[i];

                if (property == null ||
                    property.IsGroup ||
                    property.IsNativeCollectionElement)
                {
                    continue;
                }

                if (property.SerializedProperty != null)
                {
                    return true;
                }

                MemberInfo member =
                    property.Info?.MemberInfo;

                if (member is FieldInfo field)
                {
                    return !field.IsInitOnly;
                }

                if (member is PropertyInfo propertyInfo)
                {
                    return propertyInfo
                        .GetSetMethod(true) != null;
                }

                return false;
            }

            return false;
        }

        // ================================================================
        // Resolve Owner
        // ================================================================

        internal object ResolveOwnerForTarget(
            UnityEngine.Object target)
        {
            if (target == null)
            {
                return null;
            }

            AbeProperty current =
                _property.Parent;

            if (current == null)
            {
                return target;
            }

            List<AbeProperty> chain =
                new List<AbeProperty>();

            while (current != null)
            {
                chain.Add(
                    current);

                current =
                    current.Parent;
            }

            chain.Reverse();

            object owner =
                target;

            for (int i = 0;
                 i < chain.Count;
                 i++)
            {
                AbeProperty parent =
                    chain[i];

                if (parent.IsGroup)
                {
                    continue;
                }

                if (parent.IsNativeCollectionElement)
                {
                    owner =
                        parent.ValueEntry
                            .GetValueForTarget(
                                target);

                    if (owner == null)
                    {
                        return null;
                    }

                    continue;
                }

                if (parent.SerializedProperty != null)
                {
                    owner =
                        AbeReflectionUtility.GetValue(
                            target,
                            parent.SerializedProperty
                                .propertyPath);

                    if (owner == null)
                    {
                        return null;
                    }

                    continue;
                }

                MemberInfo member =
                    parent.Info?.MemberInfo;

                if (member == null)
                {
                    return null;
                }

                owner =
                    AbeReflectionUtility.GetValue(
                        owner,
                        member);

                if (owner == null)
                {
                    return null;
                }
            }

            return owner;
        }

        // ================================================================
        // Targets
        // ================================================================

        private UnityEngine.Object[] GetUnityTargets()
        {
            return (UnityEngine.Object[])(_entry.Targets
                   ?? Array.Empty<UnityEngine.Object>());
        }
    }
}