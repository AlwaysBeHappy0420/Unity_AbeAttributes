using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    [AbePropertyKindDrawer(
        AbePropertyKind.Method)]
    internal sealed class AbeMethodPropertyDrawer
        : AbePropertyKindDrawer
    {
        internal override AbePropertyKind PropertyKind =>
            AbePropertyKind.Method;

        protected override void DrawPropertyKindLayout(
            AbeProperty property,
            GUIContent label)
        {
            MethodInfo method =
                property.Info?.MethodInfo;

            if (method == null)
            {
                return;
            }

            DrawParameters(
                property,
                method);

            bool canInvoke =
                property.ValueEntry
                    .CanInvokeMethod();

            using (new EditorGUI.DisabledScope(
                !canInvoke))
            {
                if (GUILayout.Button(
                    label))
                {
                    property.ValueEntry
                        .InvokeMethod(
                            property.ValueEntry
                                .GetMethodParameterValues());
                }
            }

            if (method.ReturnType != typeof(void) &&
                property.ValueEntry.HasInvokedMethod)
            {
                DrawReturnValue(
                    property,
                    method.ReturnType);
            }
        }

        // ================================================================
        // Parameters
        // ================================================================

        private static void DrawParameters(
            AbeProperty property,
            MethodInfo method)
        {
            ParameterInfo[] parameters =
                method.GetParameters();

            if (parameters.Length == 0)
            {
                return;
            }

            object[] parameterValues =
                property.ValueEntry
                    .GetMethodParameterValues();

            EditorGUI.indentLevel++;

            for (int i = 0;
                 i < parameters.Length;
                 i++)
            {
                ParameterInfo parameter =
                    parameters[i];

                Type parameterType =
                    parameter.ParameterType;

                if (parameterType.IsByRef ||
                    parameter.IsOut)
                {
                    EditorGUILayout.LabelField(
                        parameter.Name,
                        "ref / out unsupported");

                    continue;
                }

                if (!AbeValueFieldRegistry.CanDraw(
                    parameterType))
                {
                    EditorGUILayout.LabelField(
                        parameter.Name,
                        "Unsupported parameter type");

                    continue;
                }

                object currentValue =
                    i < parameterValues.Length
                        ? parameterValues[i]
                        : null;

                object newValue =
                    AbeValueFieldRegistry.Draw(
                        new GUIContent(
                            parameter.Name),
                        currentValue,
                        parameterType);

                if (!ValuesEqual(
                    currentValue,
                    newValue))
                {
                    property.ValueEntry
                        .SetMethodParameterValue(
                            i,
                            newValue);

                    parameterValues =
                        property.ValueEntry
                            .GetMethodParameterValues();
                }
            }

            EditorGUI.indentLevel--;
        }

        // ================================================================
        // Return
        // ================================================================

        private static void DrawReturnValue(
            AbeProperty property,
            Type returnType)
        {
            IReadOnlyList<object> results =
                property.ValueEntry
                    .LastMethodResults;

            EditorGUI.indentLevel++;

            if (results == null ||
                results.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "Return",
                    "No result");

                EditorGUI.indentLevel--;

                return;
            }

            object first =
                results[0];

            bool mixed =
                false;

            for (int i = 1;
                 i < results.Count;
                 i++)
            {
                if (!ValuesEqual(
                    first,
                    results[i]))
                {
                    mixed = true;
                    break;
                }
            }

            bool oldMixedValue =
                EditorGUI.showMixedValue;

            EditorGUI.showMixedValue =
                mixed;

            using (new EditorGUI.DisabledScope(true))
            {
                if (AbeValueFieldRegistry.CanDraw(
                    returnType))
                {
                    AbeValueFieldRegistry.Draw(
                        new GUIContent("Return"),
                        first,
                        returnType);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Return",
                        first?.ToString()
                        ?? "null");
                }
            }

            EditorGUI.showMixedValue =
                oldMixedValue;

            EditorGUI.indentLevel--;
        }

        // ================================================================
        // Equality
        // ================================================================

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