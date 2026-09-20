using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace AbeAttributes.Editor
{


    public sealed class AbeProperty
    {
        private readonly List<Attribute> _attributes =
            new List<Attribute>();

        private readonly object _targetObject;

        private readonly int _collectionIndex = -1;

        private List<AbeProperty> _children;

        private AbeDrawer _drawerChain;

        internal readonly string StructureKey;

        public AbePropertyTree Tree { get; }

        public AbeProperty Parent { get; internal set; }

        public AbePropertyKind Kind { get; }

        public SerializedProperty SerializedProperty { get; }

        public InspectorPropertyInfo Info { get; }

        public AbeValueEntry ValueEntry { get; }

        public AbePropertyState State { get; } =
            new AbePropertyState();

        public AbePropertyContext Context { get; }

        public IReadOnlyList<Attribute> Attributes =>
            _attributes;

        public IReadOnlyList<AbeProperty> Children =>
            GetChildren();

        public bool IsGroup =>
            Kind == AbePropertyKind.Group;

        public bool IsNativeCollectionElement =>
            Kind ==
            AbePropertyKind.NativeCollectionElement;

        public string Name =>
            Info?.Name
            ?? SerializedProperty?.name
            ?? string.Empty;

        public object TargetObject =>
            _targetObject;

        internal int CollectionIndex =>
            _collectionIndex;

        public GUIContent DefaultLabel
        {
            get
            {
                if (SerializedProperty != null)
                {
                    return new GUIContent(
                        SerializedProperty.displayName);
                }

                if (!string.IsNullOrEmpty(Name))
                {
                    return new GUIContent(Name);
                }

                return GUIContent.none;
            }
        }

        private AbeProperty(
            AbePropertyTree tree,
            AbePropertyKind kind,
            SerializedProperty serializedProperty,
            MemberInfo memberInfo,
            Type valueType,
            string structureKey,
            IEnumerable<Attribute> attributes,
            object targetObject,
            string fallbackName = null,
            int collectionIndex = -1)
        {
            Tree =
                tree;

            Kind =
                kind;

            SerializedProperty =
                serializedProperty;

            Info =
                new InspectorPropertyInfo(
                    memberInfo,
                    valueType,
                    serializedProperty?.displayName
                    ?? fallbackName
                    ?? structureKey);

            ValueEntry =
                new AbeValueEntry(
                    this,
                    Info.ValueType);

            StructureKey =
                structureKey;

            _targetObject =
                targetObject;

            _collectionIndex =
                collectionIndex;

            Context =
                new AbePropertyContext(
                    tree,
                    structureKey);

            if (attributes != null)
            {
                _attributes.AddRange(
                    attributes);
            }
        }

        // ================================================================
        // Factory
        // ================================================================

        internal static AbeProperty CreateSerialized(
            AbePropertyTree tree,
            SerializedProperty property,
            MemberInfo memberInfo)
        {
            SerializedProperty copy =
                property.Copy();

            string key =
                "serialized:"
                + copy.propertyPath;

            object targetObject =
                AbeReflectionUtility.GetParentObject(
                    tree.Target,
                    copy.propertyPath);

            return new AbeProperty(
                tree,
                AbePropertyKind.Serialized,
                copy,
                memberInfo,
                memberInfo != null
                    ? AbeReflectionUtility.GetMemberType(
                        memberInfo)
                    : GetFallbackSerializedType(copy),
                key,
                memberInfo != null
                    ? AbeAttributeUtility
                        .GetAbeAttributes(
                            memberInfo)
                    : null,
                targetObject);
        }

        internal static AbeProperty
            CreateNonSerializedField(
                AbePropertyTree tree,
                FieldInfo field)
        {
            return CreateNonSerializedField(
                tree,
                field,
                tree.Target,
                "field:"
                + GetFieldStructureName(
                    field));
        }

        private static AbeProperty
            CreateNonSerializedField(
                AbePropertyTree tree,
                FieldInfo field,
                object targetObject,
                string structureKey)
        {
            return new AbeProperty(
                tree,
                AbePropertyKind.NonSerializedField,
                null,
                field,
                field.FieldType,
                structureKey,
                AbeAttributeUtility
                    .GetAbeAttributes(field),
                targetObject);
        }

        internal static AbeProperty
            CreateNativeProperty(
                AbePropertyTree tree,
                PropertyInfo property)
        {
            return CreateNativeProperty(
                tree,
                property,
                tree.Target,
                "property:"
                + GetMemberStructureName(
                    property));
        }

        private static AbeProperty
            CreateNativeProperty(
                AbePropertyTree tree,
                PropertyInfo property,
                object targetObject,
                string structureKey)
        {
            return new AbeProperty(
                tree,
                AbePropertyKind.NativeProperty,
                null,
                property,
                property.PropertyType,
                structureKey,
                AbeAttributeUtility
                    .GetAbeAttributes(property),
                targetObject);
        }

        internal static AbeProperty
            CreateMethod(
                AbePropertyTree tree,
                MethodInfo method)
        {
            return CreateMethod(
                tree,
                method,
                tree.Target,
                "method:"
                + GetMethodStructureName(
                    method));
        }

        private static AbeProperty
            CreateMethod(
                AbePropertyTree tree,
                MethodInfo method,
                object targetObject,
                string structureKey)
        {
            return new AbeProperty(
                tree,
                AbePropertyKind.Method,
                null,
                method,
                method.ReturnType,
                structureKey,
                AbeAttributeUtility
                    .GetAbeAttributes(
                        method),
                targetObject);
        }

        private static AbeProperty
            CreateNativeCollectionElement(
                AbeProperty parent,
                int index,
                Type valueType,
                object element)
        {
            string key =
                parent.StructureKey
                + "/element:"
                + index;

            return new AbeProperty(
                parent.Tree,
                AbePropertyKind.NativeCollectionElement,
                null,
                null,
                valueType ?? typeof(object),
                key,
                null,
                element,
                "Element " + index,
                index);
        }

        internal static AbeProperty CreateGroup(
    AbePropertyTree tree,
    GroupStartAttribute groupAttribute,
    string structureKey,
    object targetObject)
        {
            return new AbeProperty(
                tree,
                AbePropertyKind.Group,
                null,
                null,
                typeof(void),
                structureKey,
                new[] { (Attribute)groupAttribute },
                targetObject,
                groupAttribute.Name);
        }

        // ================================================================
        // Attributes
        // ================================================================

        public T GetAttribute<T>()
            where T : Attribute
        {
            for (int i = 0;
                 i < _attributes.Count;
                 i++)
            {
                if (_attributes[i] is T typed)
                {
                    return typed;
                }
            }

            return null;
        }

        public bool HasAttribute<T>()
            where T : Attribute
        {
            return GetAttribute<T>() != null;
        }

        public void AddAttribute(
            Attribute attribute)
        {
            if (attribute == null)
            {
                return;
            }

            if (attribute
                is AbeAttributes.AbeAttribute)
            {
                _attributes.Add(
                    attribute);

                _drawerChain = null;
            }
        }

        // ================================================================
        // Draw
        // ================================================================

        public void Draw(
            GUIContent label = null)
        {
            if (!Tree.ProcessRuntime(
                this))
            {
                return;
            }

            label ??= DefaultLabel;

            using (new EditorGUI.DisabledScope(
                !State.Enabled))
            {
                _drawerChain ??=
                        this.Tree
                            .DrawerLocator
                            .CreateChain(this);

                _drawerChain.Draw(
                    this,
                    label);
            }
        }

        // ================================================================
        // Children
        // ================================================================

        internal void AddChild(
            AbeProperty child)
        {
            if (child == null)
            {
                return;
            }

            child.Parent =
                this;

            if (_children == null)
            {
                _children =
                    new List<AbeProperty>();
            }

            _children.Add(
                child);
        }

        internal void ClearChildren()
        {
            _children = null;
        }

        private List<AbeProperty>
            GetChildren()
        {
            if (_children != null)
            {
                return _children;
            }

            _children =
                BuildChildren();

            for (int i = 0;
                 i < _children.Count;
                 i++)
            {
                _children[i].Parent =
                    this;
            }

            // ============================================================
            // Structural processing.
            // ============================================================

            Tree.ProcessProperties(
                _children);

            // GroupProcessor may have replaced normal children with
            // Group nodes, so assign Parent again to final top-level
            // children.
            for (int i = 0;
                 i < _children.Count;
                 i++)
            {
                _children[i].Parent =
                    this;
            }

            return _children;
        }

        private List<AbeProperty>
            BuildChildren()
        {
            switch (Kind)
            {
                case AbePropertyKind.Serialized:
                    return BuildSerializedChildren();

                case AbePropertyKind.NonSerializedField:
                case AbePropertyKind.NativeProperty:
                case AbePropertyKind.NativeCollectionElement:
                    {
                        object value =
                            ValueEntry.GetValue();

                        if (AbeCollectionUtility
                            .IsCollectionType(
                                ValueEntry.ValueType))
                        {
                            return BuildNativeCollectionChildren(
                                value);
                        }

                        return BuildObjectChildren(
                            value);
                    }

                case AbePropertyKind.Method:
                    return new List<AbeProperty>();

                case AbePropertyKind.Group:
                    return new List<AbeProperty>();

                default:
                    return new List<AbeProperty>();
            }
        }

        // ================================================================
        // Serialized Children
        // ================================================================

        private List<AbeProperty>
            BuildSerializedChildren()
        {
            List<AbeProperty> result =
                new List<AbeProperty>();

            if (SerializedProperty == null)
            {
                return result;
            }

            if (SerializedProperty.propertyPath ==
                "m_Script")
            {
                return result;
            }

            if (AbeCollectionUtility
                .IsSerializedCollection(
                    SerializedProperty))
            {
                int count =
                    SerializedProperty.arraySize;

                for (int i = 0;
                     i < count;
                     i++)
                {
                    SerializedProperty element =
                        SerializedProperty
                            .GetArrayElementAtIndex(i)
                            .Copy();

                    MemberInfo member =
                        AbeReflectionUtility.FindMember(
                            Tree.TargetType,
                            element.propertyPath);

                    AbeProperty child =
                        CreateSerialized(
                            Tree,
                            element,
                            member);

                    result.Add(
                        child);
                }

                return result;
            }

            SerializedProperty iterator =
                SerializedProperty.Copy();

            SerializedProperty end =
                SerializedProperty.GetEndProperty();

            int parentDepth =
                SerializedProperty.depth;

            if (iterator.Next(true))
            {
                while (!SerializedProperty.EqualContents(
                    iterator,
                    end))
                {
                    if (iterator.depth <= parentDepth)
                    {
                        break;
                    }

                    if (iterator.depth ==
                        parentDepth + 1)
                    {
                        SerializedProperty childProperty =
                            iterator.Copy();

                        MemberInfo member =
                            AbeReflectionUtility.FindMember(
                                Tree.TargetType,
                                childProperty.propertyPath);

                        AbeProperty child =
                            CreateSerialized(
                                Tree,
                                childProperty,
                                member);

                        result.Add(
                            child);
                    }

                    if (!iterator.Next(false))
                    {
                        break;
                    }
                }
            }

            AddNestedObjectMembers(
                result,
                ValueEntry.GetValue(),
                false);

            return result;
        }

        // ================================================================
        // Native Collection Children
        // ================================================================

        private List<AbeProperty>
            BuildNativeCollectionChildren(
                object collection)
        {
            List<AbeProperty> result =
                new List<AbeProperty>();

            if (collection == null)
            {
                return result;
            }

            int count =
                AbeCollectionUtility
                    .GetCount(
                        collection);

            if (count <= 0)
            {
                return result;
            }

            Type elementType =
                AbeCollectionUtility
                    .GetElementType(
                        ValueEntry.ValueType);

            for (int i = 0;
                 i < count;
                 i++)
            {
                object element =
                    AbeCollectionUtility
                        .GetElement(
                            collection,
                            i);

                Type actualElementType =
                    elementType;

                if (actualElementType == null &&
                    element != null)
                {
                    actualElementType =
                        element.GetType();
                }

                result.Add(
                    CreateNativeCollectionElement(
                        this,
                        i,
                        actualElementType,
                        element));
            }

            return result;
        }

        // ================================================================
        // Nested Object
        // ================================================================

        private List<AbeProperty>
            BuildObjectChildren(
                object target)
        {
            List<AbeProperty> result =
                new List<AbeProperty>();

            AddNestedObjectMembers(
                result,
                target,
                true);

            return result;
        }

        private void AddNestedObjectMembers(
            List<AbeProperty> result,
            object target,
            bool includeFields)
        {
            if (result == null ||
                target == null)
            {
                return;
            }

            if (!CanInspectNestedObject(
                target))
            {
                return;
            }

            if (IsReferenceInParentChain(
                target))
            {
                return;
            }

            if (includeFields)
            {
                AddNestedFields(
                    result,
                    target);
            }

            AddNestedNativeProperties(
                result,
                target);

            AddNestedMethods(
                result,
                target);
        }

        // ================================================================
        // Nested Fields
        // ================================================================

        private void AddNestedFields(
            List<AbeProperty> result,
            object target)
        {
            IEnumerable<FieldInfo> fields =
                AbeReflectionUtility.GetAllFields(
                    target);

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

                if (!field.IsPublic)
                {
                    continue;
                }

                if (field.Name.Contains(
                    "k__BackingField"))
                {
                    continue;
                }

                string structureKey =
                    StructureKey
                    + "/field:"
                    + GetFieldStructureName(
                        field);

                AbeProperty child =
                    CreateNonSerializedField(
                        Tree,
                        field,
                        target,
                        structureKey);

                result.Add(
                    child);
            }
        }

        // ================================================================
        // Nested Native Properties
        // ================================================================

        private void AddNestedNativeProperties(
            List<AbeProperty> result,
            object target)
        {
            IEnumerable<PropertyInfo> properties =
                AbeReflectionUtility.GetAllProperties(
                    target);

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

                if (!property.CanRead)
                {
                    continue;
                }

                MethodInfo getter =
                    property.GetGetMethod(true);

                if (getter == null)
                {
                    continue;
                }

                if (getter.IsStatic)
                {
                    continue;
                }

                if (!AbeAttributeUtility
                    .HasAbeAttribute(property))
                {
                    continue;
                }

                string structureKey =
                    StructureKey
                    + "/property:"
                    + GetMemberStructureName(
                        property);

                AbeProperty child =
                    CreateNativeProperty(
                        Tree,
                        property,
                        target,
                        structureKey);

                result.Add(
                    child);
            }
        }

        // ================================================================
        // Nested Methods
        // ================================================================

        private void AddNestedMethods(
            List<AbeProperty> result,
            object target)
        {
            IEnumerable<MethodInfo> methods =
                AbeReflectionUtility.GetAllMethods(
                    target);

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

                string structureKey =
                    StructureKey
                    + "/method:"
                    + GetMethodStructureName(
                        method);

                AbeProperty child =
                    CreateMethod(
                        Tree,
                        method,
                        target,
                        structureKey);

                result.Add(
                    child);
            }
        }

        // ================================================================
        // Nested Inspection Rules
        // ================================================================

        private static bool CanInspectNestedObject(
            object target)
        {
            if (target == null)
            {
                return false;
            }

            Type type =
                target.GetType();

            if (target is UnityEngine.Object)
            {
                return false;
            }

            if (type.IsPrimitive)
            {
                return false;
            }

            if (type.IsEnum)
            {
                return false;
            }

            if (type == typeof(string))
            {
                return false;
            }

            if (type == typeof(decimal))
            {
                return false;
            }

            if (typeof(Delegate)
                .IsAssignableFrom(type))
            {
                return false;
            }

            if (AbeCollectionUtility
                .IsCollectionType(
                    type))
            {
                return false;
            }

            return true;
        }

        // ================================================================
        // Cycle
        // ================================================================

        private bool IsReferenceInParentChain(
            object target)
        {
            if (target == null)
            {
                return false;
            }

            if (Kind !=
                AbePropertyKind.NativeCollectionElement &&
                ReferenceEquals(
                    TargetObject,
                    target))
            {
                return true;
            }

            for (AbeProperty current = Parent;
                 current != null;
                 current = current.Parent)
            {
                if (current.IsGroup)
                {
                    continue;
                }

                object parentValue =
                    current.ValueEntry.GetValue();

                if (parentValue == null)
                {
                    continue;
                }

                if (ReferenceEquals(
                    parentValue,
                    target))
                {
                    return true;
                }
            }

            return false;
        }

        // ================================================================
        // Structure Key
        // ================================================================

        private static string GetFieldStructureName(
            FieldInfo field)
        {
            string declaringType =
                field.DeclaringType != null
                    ? field.DeclaringType.FullName
                    : string.Empty;

            return declaringType
                   + "."
                   + field.Name;
        }

        private static string GetMemberStructureName(
            PropertyInfo property)
        {
            string declaringType =
                property.DeclaringType != null
                    ? property.DeclaringType.FullName
                    : string.Empty;

            return declaringType
                   + "."
                   + property.Name;
        }

        private static string GetMethodStructureName(
            MethodInfo method)
        {
            string declaringType =
                method.DeclaringType != null
                    ? method.DeclaringType.FullName
                    : string.Empty;

            ParameterInfo[] parameters =
                method.GetParameters();

            string parameterTypes =
                string.Join(
                    ",",
                    parameters
                        .Select(
                            parameter =>
                                parameter.ParameterType.FullName));

            return declaringType
                   + "."
                   + method.Name
                   + "("
                   + parameterTypes
                   + ")";
        }

        // ================================================================
        // Serialized Fallback Type
        // ================================================================

        private static Type GetFallbackSerializedType(
            SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return typeof(int);

                case SerializedPropertyType.Boolean:
                    return typeof(bool);

                case SerializedPropertyType.Float:
                    return typeof(float);

                case SerializedPropertyType.String:
                    return typeof(string);

                case SerializedPropertyType.ObjectReference:
                    return typeof(UnityEngine.Object);

                case SerializedPropertyType.Vector2:
                    return typeof(Vector2);

                case SerializedPropertyType.Vector3:
                    return typeof(Vector3);

                case SerializedPropertyType.Vector4:
                    return typeof(Vector4);

                case SerializedPropertyType.Vector2Int:
                    return typeof(Vector2Int);

                case SerializedPropertyType.Vector3Int:
                    return typeof(Vector3Int);

                case SerializedPropertyType.Color:
                    return typeof(Color);

                case SerializedPropertyType.Rect:
                    return typeof(Rect);

                case SerializedPropertyType.RectInt:
                    return typeof(RectInt);

                case SerializedPropertyType.Bounds:
                    return typeof(Bounds);

                case SerializedPropertyType.BoundsInt:
                    return typeof(BoundsInt);

                default:
                    return typeof(object);
            }
        }
    }
}