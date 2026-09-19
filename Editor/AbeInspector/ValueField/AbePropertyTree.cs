using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public sealed class AbePropertyTree : IDisposable
    {
        private readonly List<AbeProperty> _roots =
            new List<AbeProperty>();

        private readonly List<AbePropertyProcessor>
            _processors;

        private readonly List<AbeStateUpdater>
            _stateUpdaters;

        private readonly AbeValidatorProcessor
            _validatorProcessor;

        private readonly Dictionary<string, SavedBool>
            _savedBools =
                new Dictionary<string, SavedBool>();

        private readonly string _stateKeyPrefix;

        private string _serializedStructureSignature;

        internal AbeDrawerLocator DrawerLocator { get; }

        public SerializedObject SerializedObject { get; }

        public UnityEngine.Object[] Targets =>
            SerializedObject.targetObjects;

        public UnityEngine.Object Target =>
            SerializedObject.targetObject;

        public Type TargetType =>
            Target?.GetType();

        public IReadOnlyList<AbeProperty> RootProperties =>
            _roots;

        public AbePropertyTree(
            SerializedObject serializedObject)
        {
            SerializedObject =
                serializedObject
                ?? throw new ArgumentNullException(
                    nameof(serializedObject));

            _stateKeyPrefix =
                BuildStateKeyPrefix(
                    SerializedObject.targetObjects);

            DrawerLocator =
                new AbeDrawerLocator();

            _processors =
                AbePropertyProcessorLocator.CreateAll();

            _stateUpdaters =
                AbeStateUpdaterLocator.CreateAll();

            _validatorProcessor =
                new AbeValidatorProcessor();

            Build();
        }

        // ================================================================
        // Draw Lifecycle
        // ================================================================

        public void BeginDraw()
        {
            SerializedObject.Update();

            EnsureStructure();
        }

        public void Draw()
        {
            for (int i = 0;
                 i < _roots.Count;
                 i++)
            {
                AbeProperty property =
                    _roots[i];

                if (property == null)
                {
                    continue;
                }

                property.Draw();
            }
        }

        public void EndDraw()
        {
            SerializedObject.ApplyModifiedProperties();
        }

        // ================================================================
        // Runtime Property Processing
        // ================================================================

        internal bool ProcessRuntime(
            AbeProperty property)
        {
            if (property == null)
            {
                return false;
            }

            // ============================================================
            // State
            // ============================================================

            property.State.Reset();

            UpdateState(
                property);

            if (!property.State.Visible)
            {
                return false;
            }

            // ============================================================
            // Validation
            // ============================================================

            _validatorProcessor.Process(
                property);

            // ============================================================
            // Mixed Value
            // ============================================================

            property.State.HasMultipleDifferentValues =
                property.ValueEntry
                    .HasMultipleDifferentValues;

            return true;
        }

        internal void UpdateState(
            AbeProperty property)
        {
            if (property == null)
            {
                return;
            }

            for (int i = 0;
                 i < _stateUpdaters.Count;
                 i++)
            {
                AbeStateUpdater updater =
                    _stateUpdaters[i];

                if (updater == null)
                {
                    continue;
                }

                updater.Update(
                    property);
            }
        }

        // ================================================================
        // UI State
        // ================================================================

        internal SavedBool GetSavedBool(
            string propertyKey,
            string stateKey,
            bool defaultValue)
        {
            string key =
                _stateKeyPrefix
                + "|"
                + propertyKey
                + "|"
                + stateKey;

            if (_savedBools.TryGetValue(
                key,
                out SavedBool value))
            {
                return value;
            }

            value =
                new SavedBool(
                    key,
                    defaultValue);

            _savedBools.Add(
                key,
                value);

            return value;
        }

        internal void ClearSavedBools(
            string propertyKey)
        {
            if (string.IsNullOrEmpty(
                propertyKey))
            {
                return;
            }

            string prefix =
                _stateKeyPrefix
                + "|"
                + propertyKey
                + "|";

            List<string> keysToRemove =
                new List<string>();

            foreach (string key in _savedBools.Keys)
            {
                if (key.StartsWith(
                    prefix,
                    StringComparison.Ordinal))
                {
                    keysToRemove.Add(
                        key);
                }
            }

            for (int i = 0;
                 i < keysToRemove.Count;
                 i++)
            {
                _savedBools.Remove(
                    keysToRemove[i]);
            }
        }

        private static string BuildStateKeyPrefix(
            IEnumerable<UnityEngine.Object> targets)
        {
            if (targets == null)
            {
                return "Abe:none";
            }

            List<EntityId> ids =
                new List<EntityId>();

            foreach (UnityEngine.Object target
                     in targets)
            {
                if (target == null)
                {
                    continue;
                }

                ids.Add(
                    target.GetEntityId());
            }

            ids.Sort();

            if (ids.Count == 0)
            {
                return "Abe:none";
            }

            return "Abe:"
                   + string.Join(
                       ",",
                       ids);
        }

        // ================================================================
        // Build
        // ================================================================

        public void Rebuild()
        {
            Build();
        }

        private void Build()
        {
            _roots.Clear();

            BuildSerializedRoots();

            BuildNonSerializedFields();
            BuildNativeProperties();
            BuildMethods();

            ProcessProperties(
                _roots);

            _serializedStructureSignature =
                CalculateSerializedStructureSignature();
        }

        // ================================================================
        // Property Processing
        // ================================================================

        internal void ProcessProperties(
            List<AbeProperty> properties)
        {
            if (properties == null ||
                properties.Count == 0)
            {
                return;
            }

            for (int i = 0;
                 i < _processors.Count;
                 i++)
            {
                AbePropertyProcessor processor =
                    _processors[i];

                if (processor == null)
                {
                    continue;
                }

                processor.Process(
                    this,
                    properties);
            }
        }

        // ================================================================
        // Serialized
        // ================================================================

        private void BuildSerializedRoots()
        {
            SerializedProperty iterator =
                SerializedObject.GetIterator();

            if (!iterator.NextVisible(true))
            {
                return;
            }

            do
            {
                if (iterator.depth != 0)
                {
                    continue;
                }

                SerializedProperty copy =
                    iterator.Copy();

                MemberInfo member =
                    AbeReflectionUtility.FindMember(
                        TargetType,
                        copy.propertyPath);

                _roots.Add(
                    AbeProperty.CreateSerialized(
                        this,
                        copy,
                        member));

            } while (iterator.NextVisible(false));
        }

        // ================================================================
        // Non Serialized Fields
        // ================================================================

        private void BuildNonSerializedFields()
        {
            if (Target == null)
            {
                return;
            }

            IEnumerable<FieldInfo> fields =
                AbeReflectionUtility.GetAllFields(
                    Target);

            foreach (FieldInfo field in fields)
            {
                if (field == null)
                {
                    continue;
                }

                if (field.IsStatic)
                {
                    continue;
                }

                if (SerializedObject.FindProperty(
                        field.Name) != null)
                {
                    continue;
                }

                if (!field.IsPublic)
                {
                    continue;
                }

                if (field.Name.Contains(
                    "k__BackingField"))
                {
                    continue;
                }

                _roots.Add(
                    AbeProperty.CreateNonSerializedField(
                        this,
                        field));
            }
        }

        // ================================================================
        // Native Properties
        // ================================================================

        private void BuildNativeProperties()
        {
            if (Target == null)
            {
                return;
            }

            IEnumerable<PropertyInfo> properties =
                AbeReflectionUtility.GetAllProperties(
                    Target);

            foreach (PropertyInfo property in properties)
            {
                if (property == null)
                {
                    continue;
                }

                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                if (!AbeAttributeUtility
                    .HasAbeAttribute(property))
                {
                    continue;
                }

                _roots.Add(
                    AbeProperty.CreateNativeProperty(
                        this,
                        property));
            }
        }

        // ================================================================
        // Methods
        // ================================================================

        private void BuildMethods()
        {
            if (Target == null)
            {
                return;
            }

            IEnumerable<MethodInfo> methods =
                AbeReflectionUtility.GetAllMethods(
                    Target);

            foreach (MethodInfo method in methods)
            {
                if (method == null)
                {
                    continue;
                }

                if (method.IsSpecialName)
                {
                    continue;
                }

                if (!AbeAttributeUtility
                    .HasAbeAttribute(method))
                {
                    continue;
                }

                _roots.Add(
                    AbeProperty.CreateMethod(
                        this,
                        method));
            }
        }

        // ================================================================
        // Structure
        // ================================================================

        private void EnsureStructure()
        {
            string current =
                CalculateSerializedStructureSignature();

            if (!string.Equals(
                current,
                _serializedStructureSignature,
                StringComparison.Ordinal))
            {
                Build();
            }
        }

        private string
            CalculateSerializedStructureSignature()
        {
            List<string> entries =
                new List<string>();

            SerializedProperty iterator =
                SerializedObject.GetIterator();

            if (!iterator.NextVisible(true))
            {
                return string.Empty;
            }

            do
            {
                if (iterator.depth != 0)
                {
                    continue;
                }

                entries.Add(
                    iterator.propertyPath
                    + ":"
                    + iterator.propertyType
                    + ":"
                    + iterator.depth
                    + ":"
                    + iterator.hasVisibleChildren
                    + ":"
                    + iterator.isArray);

            } while (iterator.NextVisible(false));

            return string.Join(
                "|",
                entries);
        }

        // ================================================================
        // Mutable Roots
        // ================================================================

        internal List<AbeProperty> GetMutableRoots()
        {
            return _roots;
        }

        // ================================================================
        // Dispose
        // ================================================================

        public void Dispose()
        {
            for (int i = 0;
                 i < _roots.Count;
                 i++)
            {
                AbeProperty property =
                    _roots[i];

                if (property == null)
                {
                    continue;
                }

                property.Context.Clear();
            }

            _roots.Clear();

            _savedBools.Clear();
        }
    }
}