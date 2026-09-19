#if UNITY_EDITOR

using System;
using System.Reflection;
using AbeAttributes;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public sealed class TableMatrixDrawer
        : AbeAttributeDrawer<TableMatrixAttribute>
    {
        private const float CellSize = 24f;
        private const float CellSpacing = 1f;
        private const float TitleWidth = 24f;

        protected override void DrawAttributePropertyLayout(
            AbeProperty property,
            GUIContent label,
            TableMatrixAttribute attribute)
        {
            object value =
                property.ValueEntry.GetValue();

            if (value is not bool[,] matrix)
            {
                EditorGUILayout.HelpBox(
                    $"{property.Name} requires a bool[,].",
                    MessageType.Error);

                return;
            }

            if (matrix.GetLength(0) <= 0 ||
                matrix.GetLength(1) <= 0)
            {
                return;
            }

            int width =
                matrix.GetLength(0);

            int height =
                matrix.GetLength(1);

            GUILayout.Space(2);

            if (!string.IsNullOrEmpty(
                    attribute.HorizontalTitle))
            {
                EditorGUILayout.LabelField(
                    attribute.HorizontalTitle,
                    EditorStyles.miniLabel);
            }

            Rect rect =
                EditorGUILayout.GetControlRect(
                    false,
                    TitleWidth
                    + height
                    * (CellSize + CellSpacing));

            Rect matrixRect =
                new Rect(
                    rect.x + TitleWidth,
                    rect.y,
                    width
                    * (CellSize + CellSpacing),
                    height
                    * (CellSize + CellSpacing));

            bool changed = false;

            for (int y = 0;
                 y < height;
                 y++)
            {
                for (int x = 0;
                     x < width;
                     x++)
                {
                    Rect cellRect =
                        new Rect(
                            matrixRect.x
                            + x
                            * (CellSize + CellSpacing),
                            matrixRect.y
                            + y
                            * (CellSize + CellSpacing),
                            CellSize,
                            CellSize);

                    bool oldValue =
                        matrix[x, y];

                    bool newValue =
                        DrawElement(
                            property,
                            attribute,
                            cellRect,
                            oldValue);

                    if (newValue != oldValue)
                    {
                        matrix[x, y] =
                            newValue;

                        changed = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(
                    attribute.VerticalTitle))
            {
                Rect titleRect =
                    new Rect(
                        rect.x,
                        rect.y,
                        TitleWidth,
                        height
                        * (CellSize + CellSpacing));

                GUI.Label(
                    titleRect,
                    attribute.VerticalTitle,
                    EditorStyles.miniLabel);
            }

            if (changed)
            {
                property.ValueEntry.SetValue(
                    matrix);
            }
        }

        private bool DrawElement(
            AbeProperty property,
            TableMatrixAttribute attribute,
            Rect rect,
            bool value)
        {
            bool result = value;

            string methodName =
                attribute.DrawElementMethod;

            if (!string.IsNullOrEmpty(
                    methodName))
            {
                MethodInfo method =
                    AbeReflectionUtility.GetMethod(
                        property.Tree.Target,
                        methodName);

                if (method != null)
                {
                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length == 2 &&
                        parameters[0].ParameterType
                            == typeof(Rect) &&
                        parameters[1].ParameterType
                            == typeof(bool) &&
                        method.ReturnType
                            == typeof(bool))
                    {
                        object returned =
                            method.Invoke(
                                method.IsStatic
                                    ? null
                                    : property.Tree.Target,
                                new object[]
                                {
                                    rect,
                                    value
                                });

                        if (returned is bool boolValue)
                        {
                            result =
                                boolValue;
                        }

                        return result;
                    }
                }
            }

            if (Event.current.type
                    == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(
                    Event.current.mousePosition))
            {
                result = !result;

                GUI.changed = true;

                Event.current.Use();
            }

            EditorGUI.DrawRect(
                rect,
                result
                    ? new Color(
                        0.1f,
                        0.8f,
                        0.2f)
                    : new Color(
                        0f,
                        0f,
                        0f,
                        0.5f));

            return result;
        }
    }
}

#endif