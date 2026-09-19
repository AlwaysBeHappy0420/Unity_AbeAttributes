using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public sealed class ProgressBarPropertyDrawer
        : AbeAttributeDrawer<ProgressBarAttribute>
    {
        protected override void DrawAttributePropertyLayout(
            AbeProperty property,
            GUIContent label,
            ProgressBarAttribute attribute)
        {
            Type valueType =
                property.Info.ValueType;

            if (valueType != typeof(int) &&
                valueType != typeof(float))
            {
                AbeEditorGUI.HelpBox_Layout(
                    $"Field {property.Name} is not a number",
                    MessageType.Warning,
                    context: property.Tree.Target);

                return;
            }

            object value =
                property.ValueEntry.GetValue();

            object maxValue =
                GetMaxValue(
                    property,
                    attribute);

            if (maxValue == null ||
                !IsNumber(maxValue))
            {
                AbeEditorGUI.HelpBox_Layout(
                    $"The provided dynamic max value for " +
                    $"the progress bar is not correct. " +
                    $"Please check if the " +
                    $"'{nameof(attribute.MaxValueName)}' " +
                    $"is correct, or the return type is float/int",
                    MessageType.Warning,
                    context: property.Tree.Target);

                return;
            }

            float currentValue =
                CastToFloat(value);

            float max =
                CastToFloat(maxValue);

            if (Mathf.Approximately(max, 0f))
            {
                max = 1f;
            }

            float fillPercentage =
                currentValue / max;

            string valueFormatted =
                valueType == typeof(int)
                    ? ((int)currentValue).ToString()
                    : currentValue.ToString("0.00");

            string barLabel =
                (!string.IsNullOrEmpty(attribute.Name)
                    ? "[" + attribute.Name + "] "
                    : "")
                + valueFormatted
                + "/"
                + maxValue;

            Color barColor =
                attribute.Color.GetColor();

            Color labelColor =
                Color.white;

            Rect rect =
                EditorGUILayout.GetControlRect();

            float indentLength =
                AbeEditorGUI.GetIndentLength(
                    rect);

            Rect barRect =
                new Rect(
                    rect.x + indentLength,
                    rect.y,
                    rect.width - indentLength,
                    EditorGUIUtility.singleLineHeight);

            HandleInput(
                barRect,
                property,
                max);

            DrawBar(
                barRect,
                Mathf.Clamp01(fillPercentage),
                barLabel,
                barColor,
                labelColor);
        }

        private void HandleInput(
            Rect rect,
            AbeProperty property,
            float maxValue)
        {
            bool changed = false;

            const float minValue = 0f;

            object rawValue =
                property.ValueEntry.GetValue();

            float value =
                CastToFloat(rawValue);

            int controlId =
                GUIUtility.GetControlID(
                    FocusType.Keyboard);

            Event currentEvent =
                Event.current;

            bool mouseDown =
                currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0 &&
                rect.Contains(
                    currentEvent.mousePosition);

            bool mouseDrag =
                GUIUtility.hotControl == controlId &&
                (currentEvent.type == EventType.MouseMove ||
                 currentEvent.type == EventType.MouseDrag);

            if (mouseDown || mouseDrag)
            {
                GUIUtility.hotControl =
                    controlId;

                float normalized =
                    Mathf.Clamp01(
                        (currentEvent.mousePosition.x
                         - rect.xMin)
                        / rect.width);

                value =
                    minValue
                    + (maxValue - minValue)
                    * normalized;

                changed = true;
            }
            else if (
                GUIUtility.hotControl == controlId &&
                currentEvent.rawType == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
            }

            if (!changed)
            {
                return;
            }

            value =
                Mathf.Clamp(
                    value,
                    minValue,
                    maxValue);

            if (property.Info.ValueType == typeof(int))
            {
                property.ValueEntry.SetValue(
                    Mathf.RoundToInt(value));
            }
            else
            {
                property.ValueEntry.SetValue(
                    value);
            }

            GUI.changed = true;

            currentEvent.Use();
        }

        private object GetMaxValue(
            AbeProperty property,
            ProgressBarAttribute attribute)
        {
            if (string.IsNullOrEmpty(
                    attribute.MaxValueName))
            {
                return attribute.MaxValue;
            }

            object target =
                property.Tree.Target;

            if (target == null)
            {
                return null;
            }

            FieldInfo field =
                AbeReflectionUtility.FindField(
                    target.GetType(),
                    attribute.MaxValueName);

            if (field != null)
            {
                return field.GetValue(target);
            }

            PropertyInfo propertyInfo =
                AbeReflectionUtility.FindProperty(
                    target.GetType(),
                    attribute.MaxValueName);

            if (propertyInfo != null &&
                propertyInfo.GetIndexParameters().Length == 0)
            {
                return AbeReflectionUtility.GetValue(
                    target,
                    propertyInfo);
            }

            MethodInfo method =
                AbeReflectionUtility.GetMethod(
                    target,
                    attribute.MaxValueName);

            if (method != null &&
                method.GetParameters().Length == 0 &&
                (method.ReturnType == typeof(float) ||
                 method.ReturnType == typeof(int)))
            {
                return method.Invoke(
                    method.IsStatic
                        ? null
                        : target,
                    null);
            }

            return null;
        }

        private void DrawBar(
            Rect rect,
            float fillPercent,
            string label,
            Color barColor,
            Color labelColor)
        {
            if (Event.current.type !=
                EventType.Repaint)
            {
                return;
            }

            Rect fillRect =
                new Rect(
                    rect.x,
                    rect.y,
                    rect.width * fillPercent,
                    rect.height);

            EditorGUI.DrawRect(
                rect,
                new Color(
                    0.13f,
                    0.13f,
                    0.13f));

            EditorGUI.DrawRect(
                fillRect,
                barColor);

            TextAnchor oldAlignment =
                GUI.skin.label.alignment;

            Color oldContentColor =
                GUI.contentColor;

            GUI.skin.label.alignment =
                TextAnchor.UpperCenter;

            GUI.contentColor =
                labelColor;

            Rect labelRect =
                new Rect(
                    rect.x,
                    rect.y - 2f,
                    rect.width,
                    rect.height);

            EditorGUI.DropShadowLabel(
                labelRect,
                label);

            GUI.contentColor =
                oldContentColor;

            GUI.skin.label.alignment =
                oldAlignment;
        }

        private bool IsNumber(
            object value)
        {
            return
                value is int ||
                value is float;
        }

        private float CastToFloat(
            object value)
        {
            if (value is int intValue)
            {
                return intValue;
            }

            if (value is float floatValue)
            {
                return floatValue;
            }

            return 0f;
        }
    }
}