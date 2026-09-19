using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public sealed class MinMaxSliderPropertyDrawer
        : AbeAttributeDrawer<MinMaxSliderAttribute>
    {
        protected override void DrawAttributePropertyLayout(
            AbeProperty property,
            GUIContent label,
            MinMaxSliderAttribute attribute)
        {
            if (property.Info.ValueType != typeof(Vector2) &&
                property.Info.ValueType != typeof(Vector2Int))
            {
                AbeEditorGUI.HelpBox_Layout(
                    attribute.GetType().Name
                    + " can be used only on Vector2 "
                    + "or Vector2Int fields",
                    MessageType.Warning,
                    context: property.Tree.Target);

                return;
            }

            float indentLength =
                EditorGUI.indentLevel
                * 15f;

            float labelWidth =
                EditorGUIUtility.labelWidth
                + AbeEditorGUI.HorizontalSpacing;

            float floatFieldWidth =
                EditorGUIUtility.fieldWidth;

            Rect rect =
                EditorGUILayout.GetControlRect();

            float sliderWidth =
                rect.width
                - labelWidth
                - 2f * floatFieldWidth;

            const float sliderPadding = 5f;

            Rect labelRect =
                new Rect(
                    rect.x,
                    rect.y,
                    labelWidth,
                    rect.height);

            Rect sliderRect =
                new Rect(
                    rect.x
                    + labelWidth
                    + floatFieldWidth
                    + sliderPadding
                    - indentLength,
                    rect.y,
                    sliderWidth
                    - 2f * sliderPadding
                    + indentLength,
                    rect.height);

            Rect minFloatFieldRect =
                new Rect(
                    rect.x
                    + labelWidth
                    - indentLength,
                    rect.y,
                    floatFieldWidth
                    + indentLength,
                    rect.height);

            Rect maxFloatFieldRect =
                new Rect(
                    rect.x
                    + labelWidth
                    + floatFieldWidth
                    + sliderWidth
                    - indentLength,
                    rect.y,
                    floatFieldWidth
                    + indentLength,
                    rect.height);

            EditorGUI.LabelField(
                labelRect,
                label);

            if (property.Info.ValueType == typeof(Vector2))
            {
                DrawVector2(
                    property,
                    attribute,
                    sliderRect,
                    minFloatFieldRect,
                    maxFloatFieldRect);
            }
            else
            {
                DrawVector2Int(
                    property,
                    attribute,
                    sliderRect,
                    minFloatFieldRect,
                    maxFloatFieldRect);
            }
        }

        private void DrawVector2(
            AbeProperty property,
            MinMaxSliderAttribute attribute,
            Rect sliderRect,
            Rect minFieldRect,
            Rect maxFieldRect)
        {
            Vector2 value =
                property.ValueEntry.GetValue()
                is Vector2 vector
                    ? vector
                    : default;

            EditorGUI.BeginChangeCheck();

            float min =
                value.x;

            float max =
                value.y;

            EditorGUI.MinMaxSlider(
                sliderRect,
                ref min,
                ref max,
                attribute.MinValue,
                attribute.MaxValue);

            min =
                EditorGUI.FloatField(
                    minFieldRect,
                    min);

            min =
                Mathf.Clamp(
                    min,
                    attribute.MinValue,
                    Mathf.Min(
                        attribute.MaxValue,
                        max));

            max =
                EditorGUI.FloatField(
                    maxFieldRect,
                    max);

            max =
                Mathf.Clamp(
                    max,
                    Mathf.Max(
                        attribute.MinValue,
                        min),
                    attribute.MaxValue);

            if (EditorGUI.EndChangeCheck())
            {
                property.ValueEntry.SetValue(
                    new Vector2(
                        min,
                        max));
            }
        }

        private void DrawVector2Int(
            AbeProperty property,
            MinMaxSliderAttribute attribute,
            Rect sliderRect,
            Rect minFieldRect,
            Rect maxFieldRect)
        {
            Vector2Int value =
                property.ValueEntry.GetValue()
                is Vector2Int vector
                    ? vector
                    : default;

            EditorGUI.BeginChangeCheck();

            float min =
                value.x;

            float max =
                value.y;

            EditorGUI.MinMaxSlider(
                sliderRect,
                ref min,
                ref max,
                attribute.MinValue,
                attribute.MaxValue);

            int minInt =
                EditorGUI.IntField(
                    minFieldRect,
                    Mathf.RoundToInt(min));

            minInt =
                Mathf.Clamp(
                    minInt,
                    Mathf.RoundToInt(
                        attribute.MinValue),
                    Mathf.Min(
                        Mathf.RoundToInt(
                            attribute.MaxValue),
                        Mathf.RoundToInt(max)));

            int maxInt =
                EditorGUI.IntField(
                    maxFieldRect,
                    Mathf.RoundToInt(max));

            maxInt =
                Mathf.Clamp(
                    maxInt,
                    Mathf.Max(
                        Mathf.RoundToInt(
                            attribute.MinValue),
                        minInt),
                    Mathf.RoundToInt(
                        attribute.MaxValue));

            if (EditorGUI.EndChangeCheck())
            {
                property.ValueEntry.SetValue(
                    new Vector2Int(
                        minInt,
                        maxInt));
            }
        }
    }
}