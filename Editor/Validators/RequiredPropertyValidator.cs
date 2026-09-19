using System;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public sealed class RequiredPropertyValidator
        : AbePropertyValidator
    {
        public override void Validate(
            AbeProperty property)
        {
            RequiredAttribute attribute =
                property.GetAttribute<RequiredAttribute>();

            if (attribute == null)
            {
                return;
            }

            object rawValue =
                property.ValueEntry.GetValue();

            // ============================================================
            // Required
            // ============================================================

            if (IsNullOrUnityNull(rawValue))
            {
                string message =
                    string.IsNullOrEmpty(attribute.Message)
                        ? property.Name + " is required"
                        : attribute.Message;

                AbeEditorGUI.HelpBox_Layout(
                    message,
                    MessageType.Error,
                    context: property.Tree.Target);

                return;
            }

            // ============================================================
            // No required type
            // ============================================================

            if (!attribute.HasRequiredTypes)
            {
                return;
            }

            // ============================================================
            // Required type
            // ============================================================

            if (!TryGetGameObject(
                    rawValue,
                    out GameObject gameObject))
            {
                AbeEditorGUI.HelpBox_Layout(
                    "Required type validation requires "
                    + "a GameObject or Component",
                    MessageType.Warning,
                    context: property.Tree.Target);

                return;
            }

            for (int i = 0;
                 i < attribute.RequiredTypes.Length;
                 i++)
            {
                Type requiredType =
                    attribute.RequiredTypes[i];

                if (requiredType == null)
                {
                    continue;
                }

                if (gameObject.GetComponent(requiredType) != null)
                {
                    continue;
                }

                AbeEditorGUI.HelpBox_Layout(
                    property.Name
                    + " must have \""
                    + requiredType.FullName
                    + "\" or derived type",
                    MessageType.Error,
                    context: property.Tree.Target);
            }
        }

        // ====================================================================
        // Null
        // ====================================================================

        private static bool IsNullOrUnityNull(
            object value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is UnityEngine.Object unityObject)
            {
                return unityObject == null;
            }

            return false;
        }

        // ====================================================================
        // GameObject
        // ====================================================================

        private static bool TryGetGameObject(
            object value,
            out GameObject gameObject)
        {
            gameObject = null;

            if (value is GameObject directGameObject)
            {
                gameObject = directGameObject;
                return true;
            }

            if (value is Component component)
            {
                gameObject = component.gameObject;
                return true;
            }

            return false;
        }
    }
}