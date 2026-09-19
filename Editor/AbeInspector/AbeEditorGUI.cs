#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public static class AbeEditorGUI
    {
        public const float IndentLength = 15.0f;
        public const float HorizontalSpacing = 2.0f;

        // ================================================================
        // Indent
        // ================================================================

        public static float GetIndentLength(
            Rect sourceRect)
        {
            Rect indentRect =
                EditorGUI.IndentedRect(
                    sourceRect);

            return
                indentRect.x
                - sourceRect.x;
        }

        // ================================================================
        // Box Group
        // ================================================================

        public static void BeginBoxGroup_Layout(
            string label = "")
        {
            EditorGUILayout.BeginVertical(
                GUI.skin.box);

            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.LabelField(
                    label,
                    EditorStyles.boldLabel);
            }
        }

        public static void EndBoxGroup_Layout()
        {
            EditorGUILayout.EndVertical();
        }

        // ================================================================
        // Horizontal Line
        // ================================================================

        public static void HorizontalLine(
            Rect rect,
            float height,
            Color color)
        {
            rect.height = height;

            EditorGUI.DrawRect(
                rect,
                color);
        }

        // ================================================================
        // HelpBox
        // ================================================================

        public static void HelpBox(
            Rect rect,
            string message,
            MessageType type,
            UnityEngine.Object context = null,
            bool logToConsole = false)
        {
            EditorGUI.HelpBox(
                rect,
                message,
                type);

            if (logToConsole)
            {
                DebugLogMessage(
                    message,
                    type,
                    context);
            }
        }

        public static void HelpBox_Layout(
            string message,
            MessageType type,
            UnityEngine.Object context = null,
            bool logToConsole = false)
        {
            EditorGUILayout.HelpBox(
                message,
                type);

            if (logToConsole)
            {
                DebugLogMessage(
                    message,
                    type,
                    context);
            }
        }

        // ================================================================
        // Readonly Field
        // ================================================================

        public static bool Field_Layout(
            object value,
            string label)
        {
            if (value == null)
            {
                EditorGUILayout.LabelField(
                    label,
                    "<Null>");

                return true;
            }

            using (
                new EditorGUI.DisabledScope(
                    disabled: true))
            {
                Type valueType =
                    value.GetType();

                if (valueType == typeof(bool))
                {
                    EditorGUILayout.Toggle(
                        label,
                        (bool)value);
                }
                else if (valueType == typeof(short))
                {
                    EditorGUILayout.IntField(
                        label,
                        (short)value);
                }
                else if (valueType == typeof(ushort))
                {
                    EditorGUILayout.IntField(
                        label,
                        (ushort)value);
                }
                else if (valueType == typeof(int))
                {
                    EditorGUILayout.IntField(
                        label,
                        (int)value);
                }
                else if (valueType == typeof(uint))
                {
                    EditorGUILayout.LongField(
                        label,
                        (uint)value);
                }
                else if (valueType == typeof(long))
                {
                    EditorGUILayout.LongField(
                        label,
                        (long)value);
                }
                else if (valueType == typeof(ulong))
                {
                    EditorGUILayout.TextField(
                        label,
                        ((ulong)value).ToString());
                }
                else if (valueType == typeof(float))
                {
                    EditorGUILayout.FloatField(
                        label,
                        (float)value);
                }
                else if (valueType == typeof(double))
                {
                    EditorGUILayout.DoubleField(
                        label,
                        (double)value);
                }
                else if (valueType == typeof(string))
                {
                    EditorGUILayout.TextField(
                        label,
                        (string)value);
                }
                else if (valueType == typeof(Vector2))
                {
                    EditorGUILayout.Vector2Field(
                        label,
                        (Vector2)value);
                }
                else if (valueType == typeof(Vector3))
                {
                    EditorGUILayout.Vector3Field(
                        label,
                        (Vector3)value);
                }
                else if (valueType == typeof(Vector4))
                {
                    EditorGUILayout.Vector4Field(
                        label,
                        (Vector4)value);
                }
                else if (valueType == typeof(Vector2Int))
                {
                    EditorGUILayout.Vector2IntField(
                        label,
                        (Vector2Int)value);
                }
                else if (valueType == typeof(Vector3Int))
                {
                    EditorGUILayout.Vector3IntField(
                        label,
                        (Vector3Int)value);
                }
                else if (valueType == typeof(Color))
                {
                    EditorGUILayout.ColorField(
                        label,
                        (Color)value);
                }
                else if (valueType == typeof(Bounds))
                {
                    EditorGUILayout.BoundsField(
                        label,
                        (Bounds)value);
                }
                else if (valueType == typeof(Rect))
                {
                    EditorGUILayout.RectField(
                        label,
                        (Rect)value);
                }
                else if (valueType == typeof(RectInt))
                {
                    EditorGUILayout.RectIntField(
                        label,
                        (RectInt)value);
                }
                else if (
                    typeof(UnityEngine.Object)
                        .IsAssignableFrom(
                            valueType))
                {
                    EditorGUILayout.ObjectField(
                        label,
                        (UnityEngine.Object)value,
                        valueType,
                        true);
                }
                else if (
                    valueType.IsEnum)
                {
                    EditorGUILayout.EnumPopup(
                        label,
                        (Enum)value);
                }
                else
                {
                    return false;
                }

                return true;
            }
        }

        // ================================================================
        // Debug
        // ================================================================

        private static void DebugLogMessage(
            string message,
            MessageType type,
            UnityEngine.Object context)
        {
            switch (type)
            {
                case MessageType.None:
                case MessageType.Info:

                    Debug.Log(
                        message,
                        context);

                    break;

                case MessageType.Warning:

                    Debug.LogWarning(
                        message,
                        context);

                    break;

                case MessageType.Error:

                    Debug.LogError(
                        message,
                        context);

                    break;
            }
        }
    }
}

#endif