using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace AbeAttributes.Editor
{
    internal sealed class AbeCollectionAccessor
    {
        private readonly AbeValueEntry _entry;
        private readonly AbeProperty _property;

        private readonly Func<
            UnityEngine.Object,
            object,
            bool>
            _setPropertyChainValueForTarget;

        private readonly Func<bool>
            _canWriteCollectionMember;

        public AbeCollectionAccessor(
            AbeValueEntry entry,
            AbeProperty property,
            Func<
                UnityEngine.Object,
                object,
                bool>
                setPropertyChainValueForTarget,
            Func<bool>
                canWriteCollectionMember)
        {
            _entry =
                entry;

            _property =
                property;

            _setPropertyChainValueForTarget =
                setPropertyChainValueForTarget;

            _canWriteCollectionMember =
                canWriteCollectionMember;
        }

        // ================================================================
        // Element Get
        // ================================================================

        public object GetElementForTarget(
            UnityEngine.Object target,
            int index)
        {
            if (target == null ||
                index < 0)
            {
                return null;
            }

            AbeProperty parent =
                _property.Parent;

            if (parent == null)
            {
                return null;
            }

            object collection =
                parent.ValueEntry
                    .GetValueForTarget(
                        target);

            return AbeCollectionUtility
                .GetElement(
                    collection,
                    index);
        }

        // ================================================================
        // Element Write Permission
        // ================================================================

        public bool CanWriteElement()
        {
            AbeProperty parent =
                _property.Parent;

            if (parent == null)
            {
                return false;
            }

            IReadOnlyList<UnityEngine.Object> targets =
                _entry.Targets;

            if (targets == null ||
                targets.Count == 0)
            {
                return false;
            }

            for (int i = 0;
                 i < targets.Count;
                 i++)
            {
                UnityEngine.Object target =
                    targets[i];

                if (target == null)
                {
                    return false;
                }

                object collection =
                    parent.ValueEntry
                        .GetValueForTarget(
                            target);

                if (!AbeCollectionUtility
                    .CanWriteElement(
                        collection,
                        _property.CollectionIndex))
                {
                    return false;
                }
            }

            return true;
        }

        // ================================================================
        // Element Set
        // ================================================================

        public bool SetElement(
            object collection,
            int index,
            object value)
        {
            return AbeCollectionUtility
                .SetElement(
                    collection,
                    index,
                    value);
        }

        // ================================================================
        // Count
        // ================================================================

        public int GetCount()
        {
            return GetCount(
                out _);
        }

        public int GetCount(
            out bool hasDifferentSizes)
        {
            hasDifferentSizes =
                false;

            IReadOnlyList<UnityEngine.Object> targets =
                _entry.Targets;

            if (targets == null ||
                targets.Count == 0)
            {
                return 0;
            }

            int firstCount = -1;
            bool hasFirstCount = false;

            for (int i = 0;
                 i < targets.Count;
                 i++)
            {
                UnityEngine.Object target =
                    targets[i];

                if (target == null)
                {
                    continue;
                }

                object collection =
                    _entry.GetValueForTarget(
                        target);

                int count =
                    AbeCollectionUtility.GetCount(
                        collection);

                if (!hasFirstCount)
                {
                    firstCount =
                        count;

                    hasFirstCount =
                        true;

                    continue;
                }

                if (count != firstCount)
                {
                    hasDifferentSizes =
                        true;

                    break;
                }
            }

            if (!hasFirstCount)
            {
                return 0;
            }

            return firstCount;
        }

        // ================================================================
        // Resize
        // ================================================================

        public bool CanResize()
        {
            IReadOnlyList<UnityEngine.Object> targets =
                _entry.Targets;

            if (targets == null ||
                targets.Count == 0)
            {
                return false;
            }

            for (int i = 0;
                 i < targets.Count;
                 i++)
            {
                UnityEngine.Object target =
                    targets[i];

                if (target == null)
                {
                    return false;
                }

                object collection =
                    _entry.GetValueForTarget(
                        target);

                if (collection == null)
                {
                    return false;
                }

                if (collection is Array)
                {
                    if (!_canWriteCollectionMember())
                    {
                        return false;
                    }

                    continue;
                }

                if (collection is IList list)
                {
                    if (list.IsReadOnly ||
                        list.IsFixedSize)
                    {
                        return false;
                    }

                    continue;
                }

                return false;
            }

            return true;
        }

        public bool TrySetSize(
            int newSize)
        {
            if (newSize < 0 ||
                !CanResize())
            {
                return false;
            }

            IReadOnlyList<UnityEngine.Object> targets =
                _entry.Targets;

            if (targets == null ||
                targets.Count == 0)
            {
                return false;
            }

            UnityEngine.Object[] unityTargets =
                GetUnityTargets();

            if (unityTargets.Length > 0)
            {
                Undo.RecordObjects(
                    unityTargets,
                    "Resize "
                    + _property.Name);
            }

            bool changedAny =
                false;

            for (int i = 0;
                 i < targets.Count;
                 i++)
            {
                UnityEngine.Object target =
                    targets[i];

                if (target == null)
                {
                    continue;
                }

                object collection =
                    _entry.GetValueForTarget(
                        target);

                if (collection == null)
                {
                    continue;
                }

                // ========================================================
                // Array
                // ========================================================

                if (collection is Array array)
                {
                    int oldSize =
                        array.Length;

                    if (oldSize ==
                        newSize)
                    {
                        continue;
                    }

                    Type elementType =
                        AbeCollectionUtility
                            .GetElementType(
                                array.GetType());

                    if (elementType == null)
                    {
                        continue;
                    }

                    Array resized =
                        Array.CreateInstance(
                            elementType,
                            newSize);

                    int copyCount =
                        Math.Min(
                            oldSize,
                            newSize);

                    if (copyCount > 0)
                    {
                        Array.Copy(
                            array,
                            resized,
                            copyCount);
                    }

                    for (int index =
                             copyCount;
                         index < newSize;
                         index++)
                    {
                        resized.SetValue(
                            AbeCollectionUtility
                                .CreateDefaultElement(
                                    elementType),
                            index);
                    }

                    bool changed =
                        _setPropertyChainValueForTarget(
                            target,
                            resized);

                    if (changed)
                    {
                        changedAny =
                            true;

                        EditorUtility.SetDirty(
                            target);
                    }

                    continue;
                }

                // ========================================================
                // IList
                // ========================================================

                if (collection is IList list)
                {
                    if (list.IsReadOnly ||
                        list.IsFixedSize)
                    {
                        continue;
                    }

                    Type elementType =
                        AbeCollectionUtility
                            .GetElementType(
                                _entry.ValueType);

                    if (elementType == null)
                    {
                        elementType =
                            typeof(object);
                    }

                    int oldSize =
                        list.Count;

                    if (oldSize ==
                        newSize)
                    {
                        continue;
                    }

                    while (list.Count <
                           newSize)
                    {
                        list.Add(
                            AbeCollectionUtility
                                .CreateDefaultElement(
                                    elementType));
                    }

                    while (list.Count >
                           newSize)
                    {
                        list.RemoveAt(
                            list.Count - 1);
                    }

                    changedAny =
                        true;

                    EditorUtility.SetDirty(
                        target);
                }
            }

            return changedAny;
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