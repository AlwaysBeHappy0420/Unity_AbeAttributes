using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AbeAttributes.Editor
{
    internal sealed class AbeDrawerLocator
    {
        private readonly List<AbeDrawerDescriptor>
            _descriptors;

        public AbeDrawerLocator()
        {
            _descriptors =
                AbeTypeScanner
                    .FindConcreteTypes<AbeDrawer>()
                    .Select(
                        AbeDrawerDescriptor.Create)
                    .Where(
                        descriptor =>
                            descriptor != null)
                    .ToList();
        }

        // ================================================================
        // Create Chain
        // ================================================================

        public AbeDrawer CreateChain(
            AbeProperty property)
        {
            if (property == null)
            {
                return new AbeDefaultDrawer();
            }

            List<AbeDrawerDescriptor>
                matchedDescriptors =
                    GetMatchedDescriptors(
                        property);

            List<AbeDrawer> drawers =
                new List<AbeDrawer>(
                    matchedDescriptors.Count + 1);

            for (int i = 0;
                 i < matchedDescriptors.Count;
                 i++)
            {
                AbeDrawerDescriptor descriptor =
                    matchedDescriptors[i];

                AbeDrawer drawer =
                    descriptor.Create();

                if (drawer == null)
                {
                    continue;
                }

                drawers.Add(
                    drawer);
            }

            // ============================================================
            // Default Drawer
            // ============================================================

            drawers.Add(
                new AbeDefaultDrawer());

            // ============================================================
            // Link Chain
            // ============================================================

            for (int i = 0;
                 i < drawers.Count - 1;
                 i++)
            {
                drawers[i].Next =
                    drawers[i + 1];
            }

            return drawers[0];
        }

        // ================================================================
        // Matching + Ordering
        // ================================================================

        private List<AbeDrawerDescriptor>
            GetMatchedDescriptors(
                AbeProperty property)
        {
            List<AbeDrawerDescriptor> result =
                new List<AbeDrawerDescriptor>();

            for (int i = 0;
                 i < _descriptors.Count;
                 i++)
            {
                AbeDrawerDescriptor descriptor =
                    _descriptors[i];

                if (descriptor == null)
                {
                    continue;
                }

                if (!descriptor.Matches(
                    property))
                {
                    continue;
                }

                result.Add(
                    descriptor);
            }

            if (property.IsGroup)
            {
                result.Sort(
                    CompareGroupDrawers);

                return result;
            }

            result.Sort(
                CompareNormalDrawers);

            return result;
        }

        // ================================================================
        // Normal
        // ================================================================

        private static int CompareNormalDrawers(
            AbeDrawerDescriptor a,
            AbeDrawerDescriptor b)
        {
            int kindComparison =
                GetNormalKindOrder(
                    a.Kind)
                .CompareTo(
                    GetNormalKindOrder(
                        b.Kind));

            if (kindComparison != 0)
            {
                return kindComparison;
            }

            int priorityComparison =
                b.Priority.CompareTo(
                    a.Priority);

            if (priorityComparison != 0)
            {
                return priorityComparison;
            }

            return string.Compare(
                a.DrawerType.FullName,
                b.DrawerType.FullName,
                StringComparison.Ordinal);
        }

        private static int GetNormalKindOrder(
            AbeDrawerKind kind)
        {
            switch (kind)
            {
                case AbeDrawerKind.Attribute:
                    return 0;

                case AbeDrawerKind.Value:
                    return 1;

                case AbeDrawerKind.PropertyKind:
                    return 2;

                default:
                    return 3;
            }
        }

        // ================================================================
        // Group
        // ================================================================

        private static int CompareGroupDrawers(
            AbeDrawerDescriptor a,
            AbeDrawerDescriptor b)
        {
            int kindComparison =
                GetGroupKindOrder(
                    a.Kind)
                .CompareTo(
                    GetGroupKindOrder(
                        b.Kind));

            if (kindComparison != 0)
            {
                return kindComparison;
            }

            int priorityComparison =
                b.Priority.CompareTo(
                    a.Priority);

            if (priorityComparison != 0)
            {
                return priorityComparison;
            }

            return string.Compare(
                a.DrawerType.FullName,
                b.DrawerType.FullName,
                StringComparison.Ordinal);
        }

        private static int GetGroupKindOrder(
            AbeDrawerKind kind)
        {
            switch (kind)
            {
                case AbeDrawerKind.Group:
                    return 0;

                case AbeDrawerKind.Attribute:
                    return 1;

                case AbeDrawerKind.PropertyKind:
                    return 2;

                case AbeDrawerKind.Value:
                    return 3;

                default:
                    return 4;
            }
        }

        // ================================================================
        // Drawer Kind
        // ================================================================

        private enum AbeDrawerKind
        {
            Unknown,
            Value,
            Attribute,
            Group,
            PropertyKind
        }

        // ================================================================
        // Descriptor
        // ================================================================

        private sealed class AbeDrawerDescriptor
        {
            private readonly Type _drawerType;
            private readonly Type _valueType;
            private readonly Type _attributeType;
            private readonly Type _groupAttributeType;
            private readonly AbePropertyKind?
                _propertyKind;
            private readonly AbeDrawerKind _kind;
            private readonly int _priority;

            public Type DrawerType =>
                _drawerType;

            public AbeDrawerKind Kind =>
                _kind;

            public int Priority =>
                _priority;

            private AbeDrawerDescriptor(
                Type drawerType,
                AbeDrawerKind kind,
                Type valueType,
                Type attributeType,
                Type groupAttributeType,
                AbePropertyKind? propertyKind,
                int priority)
            {
                _drawerType =
                    drawerType;

                _kind =
                    kind;

                _valueType =
                    valueType;

                _attributeType =
                    attributeType;

                _groupAttributeType =
                    groupAttributeType;

                _propertyKind =
                    propertyKind;

                _priority =
                    priority;
            }

            // ============================================================
            // Create
            // ============================================================

            public static AbeDrawerDescriptor Create(
                Type drawerType)
            {
                if (drawerType == null ||
                    drawerType.IsAbstract ||
                    drawerType.IsInterface)
                {
                    return null;
                }

                AbeDrawerPriorityAttribute priority =
                    drawerType.GetCustomAttribute<
                        AbeDrawerPriorityAttribute>();

                int priorityValue =
                    priority?.Value ?? 0;

                // ========================================================
                // Property Kind Drawer
                // ========================================================

                AbePropertyKindDrawerAttribute
                    propertyKindAttribute =
                        drawerType.GetCustomAttribute<
                            AbePropertyKindDrawerAttribute>();

                if (propertyKindAttribute != null)
                {
                    return new AbeDrawerDescriptor(
                        drawerType,
                        AbeDrawerKind.PropertyKind,
                        null,
                        null,
                        null,
                        propertyKindAttribute.Kind,
                        priorityValue);
                }

                // ========================================================
                // Group Drawer
                // ========================================================

                Type groupBase =
                    GetGenericBase(
                        drawerType,
                        typeof(AbeGroupDrawer<>));

                if (groupBase != null)
                {
                    return new AbeDrawerDescriptor(
                        drawerType,
                        AbeDrawerKind.Group,
                        null,
                        null,
                        groupBase.GetGenericArguments()[0],
                        null,
                        priorityValue);
                }

                // ========================================================
                // Attribute Drawer
                // ========================================================

                Type attributeBase =
                    GetGenericBase(
                        drawerType,
                        typeof(AbeAttributeDrawer<>));

                if (attributeBase != null)
                {
                    return new AbeDrawerDescriptor(
                        drawerType,
                        AbeDrawerKind.Attribute,
                        null,
                        attributeBase.GetGenericArguments()[0],
                        null,
                        null,
                        priorityValue);
                }

                // ========================================================
                // Value Drawer
                // ========================================================

                Type valueBase =
                    GetGenericBase(
                        drawerType,
                        typeof(AbeValueDrawer<>));

                if (valueBase != null)
                {
                    return new AbeDrawerDescriptor(
                        drawerType,
                        AbeDrawerKind.Value,
                        valueBase.GetGenericArguments()[0],
                        null,
                        null,
                        null,
                        priorityValue);
                }

                return null;
            }

            // ============================================================
            // Matches
            // ============================================================

            public bool Matches(
                AbeProperty property)
            {
                if (property == null)
                {
                    return false;
                }

                switch (_kind)
                {
                    case AbeDrawerKind.PropertyKind:
                        return MatchesPropertyKind(
                            property);

                    case AbeDrawerKind.Group:
                        return MatchesGroup(
                            property);

                    case AbeDrawerKind.Attribute:
                        return MatchesAttribute(
                            property);

                    case AbeDrawerKind.Value:
                        return MatchesValue(
                            property);

                    default:
                        return false;
                }
            }

            private bool MatchesPropertyKind(
                AbeProperty property)
            {
                return _propertyKind.HasValue &&
                       property.Kind ==
                       _propertyKind.Value;
            }

            private bool MatchesGroup(
                AbeProperty property)
            {
                if (!property.IsGroup)
                {
                    return false;
                }

                if (_groupAttributeType == null)
                {
                    return false;
                }

                IReadOnlyList<Attribute> attributes =
                    property.Attributes;

                for (int i = 0;
                     i < attributes.Count;
                     i++)
                {
                    Attribute attribute =
                        attributes[i];

                    if (attribute == null)
                    {
                        continue;
                    }

                    if (_groupAttributeType.IsAssignableFrom(
                        attribute.GetType()))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool MatchesAttribute(
                AbeProperty property)
            {
                if (_attributeType == null)
                {
                    return false;
                }

                IReadOnlyList<Attribute> attributes =
                    property.Attributes;

                for (int i = 0;
                     i < attributes.Count;
                     i++)
                {
                    Attribute attribute =
                        attributes[i];

                    if (attribute == null)
                    {
                        continue;
                    }

                    if (_attributeType.IsAssignableFrom(
                        attribute.GetType()))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool MatchesValue(
                AbeProperty property)
            {
                if (_valueType == null)
                {
                    return false;
                }

                Type actualType =
                    property.ValueEntry.ValueType
                    ?? typeof(object);

                return _valueType.IsAssignableFrom(
                    actualType);
            }

            // ============================================================
            // Instantiate
            // ============================================================

            public AbeDrawer Create()
            {
                try
                {
                    return (AbeDrawer)
                        Activator.CreateInstance(
                            _drawerType);
                }
                catch
                {
                    return null;
                }
            }

            // ============================================================
            // Generic Base
            // ============================================================

            private static Type GetGenericBase(
                Type type,
                Type genericDefinition)
            {
                Type current =
                    type;

                while (current != null &&
                       current != typeof(object))
                {
                    if (current.IsGenericType &&
                        current.GetGenericTypeDefinition()
                            == genericDefinition)
                    {
                        return current;
                    }

                    current =
                        current.BaseType;
                }

                return null;
            }
        }
    }
}