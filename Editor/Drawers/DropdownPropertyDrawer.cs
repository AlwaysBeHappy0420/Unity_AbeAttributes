using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public sealed class DropdownPropertyDrawer
        : AbeAttributeDrawer<DropdownAttribute>
    {
        protected override void DrawAttributePropertyLayout(
            AbeProperty property,
            GUIContent label,
            DropdownAttribute attribute)
        {
            object[] values =
                GetValues(
                    property,
                    attribute.ValuesName);

            if (values == null)
            {
                AbeEditorGUI.HelpBox_Layout(
                    $"Invalid values with name " +
                    $"'{attribute.ValuesName}' provided to " +
                    $"'DropdownAttribute'. " +
                    $"Either the values name is incorrect " +
                    $"or the types are not compatible.",
                    MessageType.Warning,
                    context: property.Tree.Target);

                return;
            }

            if (values.Length == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup(
                        label,
                        -1,
                        new[] { "<No Values>" });
                }

                return;
            }

            GUIContent[] options =
                BuildGUIContents(
                    values);

            object currentValue =
                property.ValueEntry.GetValue();

            int selectedIndex =
                FindValueIndex(
                    currentValue,
                    values);

            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            int newIndex =
                EditorGUILayout.Popup(
                    label,
                    selectedIndex,
                    options);

            if (newIndex == selectedIndex)
            {
                return;
            }

            if (newIndex < 0 ||
                newIndex >= values.Length)
            {
                return;
            }

            property.ValueEntry.SetValue(
                values[newIndex]);
        }

        private object[] GetValues(
            AbeProperty property,
            string valuesName)
        {
            if (string.IsNullOrEmpty(valuesName))
            {
                return null;
            }

            object target =
                property.TargetObject;

            if (target == null)
            {
                return null;
            }

            if (!AbeReflectionUtility.TryGetMemberValue(
                    target,
                    valuesName,
                    out object value,
                    out _))
            {
                return null;
            }

            return ConvertValues(
                value);
        }

        private object[] ConvertValues(
            object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is IList list)
            {
                object[] result =
                    new object[list.Count];

                for (int i = 0;
                     i < list.Count;
                     i++)
                {
                    result[i] =
                        list[i];
                }

                return result;
            }

            if (value is IEnumerable enumerable &&
                !(value is string))
            {
                List<object> result =
                    new List<object>();

                foreach (object item in enumerable)
                {
                    result.Add(item);
                }

                return result.ToArray();
            }

            return null;
        }

        private GUIContent[] BuildGUIContents(
            object[] values)
        {
            GUIContent[] result =
                new GUIContent[values.Length];

            for (int i = 0;
                 i < values.Length;
                 i++)
            {
                result[i] =
                    new GUIContent(
                        values[i]?.ToString()
                        ?? "<Null>");
            }

            return result;
        }

        private int FindValueIndex(
            object currentValue,
            object[] values)
        {
            if (currentValue == null)
            {
                return -1;
            }

            for (int i = 0;
                 i < values.Length;
                 i++)
            {
                if (ValuesEqual(
                        currentValue,
                        values[i]))
                {
                    return i;
                }
            }

            return -1;
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

            if (a.Equals(b))
            {
                return true;
            }

            if (IsNumeric(a) &&
                IsNumeric(b))
            {
                try
                {
                    return
                        Convert.ToDouble(a)
                        ==
                        Convert.ToDouble(b);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static bool IsNumeric(
            object value)
        {
            return
                value is byte ||
                value is sbyte ||
                value is short ||
                value is ushort ||
                value is int ||
                value is uint ||
                value is long ||
                value is ulong ||
                value is float ||
                value is double ||
                value is decimal;
        }
    }
}