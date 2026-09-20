using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    internal sealed class AbeDefaultDrawer
        : AbeDrawer
    {
        protected override void DrawPropertyLayout(
            AbeProperty property,
            GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            switch (property.Kind)
            {
                case AbePropertyKind.NativeProperty:
                    DrawNativeProperty(
                        property,
                        label);

                    return;

                case AbePropertyKind.NonSerializedField:
                case AbePropertyKind.NativeCollectionElement:
                    DrawValue(
                        property,
                        label);

                    return;

                default:
                    return;
            }
        }

        private void DrawNativeProperty(
            AbeProperty property,
            GUIContent label)
        {
            PropertyInfo propertyInfo =
                property.Info?.PropertyInfo;

            if (propertyInfo == null)
            {
                DrawValue(
                    property,
                    label);

                return;
            }

            if (HasPopToConsoleManual(
                    propertyInfo))
            {
                DrawPopToConsoleManual(
                    property,
                    propertyInfo);
            }

            DrawValue(
                property,
                label);
        }

        private static bool HasPopToConsoleManual(
            PropertyInfo propertyInfo)
        {
            object[] attributes =
                propertyInfo.GetCustomAttributes(
                    typeof(PopToConsoleAttribute),
                    true);

            for (int i = 0;
                 i < attributes.Length;
                 i++)
            {
                if (!(attributes[i]
                      is PopToConsoleAttribute attribute))
                {
                    continue;
                }

                if (attribute.Mode ==
                    PopToConsoleMode.Manual)
                {
                    return true;
                }
            }

            return false;
        }

        private static void DrawPopToConsoleManual(
    AbeProperty property,
    PropertyInfo propertyInfo)
        {
            if (!GUILayout.Button(
                    "Pop To Console"))
            {
                return;
            }

            object value =
                property.ValueEntry.GetValue();

            PopToConsoleRuntime.OnManual(
                propertyInfo.DeclaringType?.FullName
                ?? "<UnknownType>",
                propertyInfo.Name,
                value);
        }

        private void DrawValue(
            AbeProperty property,
            GUIContent label)
        {
            Type valueType =
                property.ValueEntry.ValueType;

            if (valueType == null)
            {
                EditorGUILayout.LabelField(
                    label,
                    "null");

                return;
            }

            if (AbeCollectionUtility
                .IsCollectionType(
                    valueType))
            {
                return;
            }

            object value =
                property.ValueEntry.GetValue();

            IReadOnlyList<AbeProperty> children =
                property.Children;

            if (CanDrawNestedObject(
                    value,
                    children))
            {
                DrawNestedObject(
                    property,
                    label,
                    children);

                return;
            }

            DrawDirectValue(
                property,
                label,
                value,
                valueType);
        }

        private static void DrawDirectValue(
            AbeProperty property,
            GUIContent label,
            object value,
            Type valueType)
        {
            bool mixed =
                property.ValueEntry
                    .HasMultipleDifferentValues;

            bool canWrite =
                property.ValueEntry.CanWrite;

            bool oldMixedValue =
                EditorGUI.showMixedValue;

            EditorGUI.showMixedValue =
                mixed;

            object newValue;

            using (new EditorGUI.DisabledScope(
                !canWrite))
            {
                if (AbeValueFieldRegistry.CanDraw(
                        valueType))
                {
                    newValue =
                        AbeValueFieldRegistry.Draw(
                            label,
                            value,
                            valueType);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        label,
                        new GUIContent(
                            value?.ToString()
                            ?? "null"));

                    newValue =
                        value;
                }
            }

            EditorGUI.showMixedValue =
                oldMixedValue;

            if (!ValuesEqual(
                    value,
                    newValue))
            {
                property.ValueEntry.SetValue(
                    newValue);
            }
        }

        private static void DrawNestedObject(
            AbeProperty property,
            GUIContent label,
            IReadOnlyList<AbeProperty> children)
        {
            SavedBool expandedState =
                property.Context.GetSavedBool(
                    "nestedObject.expanded",
                    false);

            bool expanded =
                expandedState.value;

            Rect rect =
                EditorGUILayout.GetControlRect(
                    true,
                    EditorGUIUtility.singleLineHeight);

            bool newExpanded =
                EditorGUI.Foldout(
                    rect,
                    expanded,
                    label,
                    true);

            if (newExpanded != expanded)
            {
                expandedState.value =
                    newExpanded;

                expanded =
                    newExpanded;
            }

            if (!expanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            for (int i = 0;
                 i < children.Count;
                 i++)
            {
                children[i].Draw();
            }

            EditorGUI.indentLevel--;
        }

        private static bool CanDrawNestedObject(
            object value,
            IReadOnlyList<AbeProperty> children)
        {
            if (value == null)
            {
                return false;
            }

            if (children == null ||
                children.Count == 0)
            {
                return false;
            }

            Type type =
                value.GetType();

            if (AbeValueFieldRegistry.CanDraw(
                    type))
            {
                return false;
            }

            if (AbeCollectionUtility
                .IsCollectionType(
                    type))
            {
                return false;
            }

            if (typeof(
                    System.Collections.IEnumerable)
                .IsAssignableFrom(type))
            {
                return false;
            }

            if (typeof(Delegate)
                .IsAssignableFrom(type))
            {
                return false;
            }

            return true;
        }

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