using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public sealed class ValidateInputPropertyValidator
        : AbePropertyValidator
    {
        public override void Validate(
            AbeProperty property)
        {
            if (property == null)
            {
                return;
            }

            ValidateInputAttribute attribute =
                property.GetAttribute<
                    ValidateInputAttribute>();

            if (attribute == null)
            {
                return;
            }

            object target =
                property.TargetObject;

            if (target == null)
            {
                return;
            }

            MethodInfo method =
                AbeReflectionUtility.GetMethod(
                    target,
                    attribute.CallbackName);

            if (method == null)
            {
                AbeEditorGUI.HelpBox_Layout(
                    $"ValidateInput callback " +
                    $"'{attribute.CallbackName}' " +
                    $"was not found on " +
                    $"{target.GetType().Name}.",
                    MessageType.Error);

                return;
            }

            ParameterInfo[] parameters =
                method.GetParameters();

            if (method.ReturnType != typeof(bool))
            {
                AbeEditorGUI.HelpBox_Layout(
                    $"ValidateInput callback " +
                    $"'{method.Name}' must return bool.",
                    MessageType.Error);

                return;
            }

            if (parameters.Length == 0)
            {
                ValidateWithoutParameter(
                    property,
                    method,
                    target,
                    attribute);

                return;
            }

            if (parameters.Length == 1)
            {
                ValidateWithParameter(
                    property,
                    method,
                    target,
                    parameters[0],
                    attribute);

                return;
            }

            AbeEditorGUI.HelpBox_Layout(
                $"ValidateInput callback " +
                $"'{method.Name}' must have " +
                $"zero or one parameter.",
                MessageType.Error);
        }

        private void ValidateWithoutParameter(
            AbeProperty property,
            MethodInfo method,
            object target,
            ValidateInputAttribute attribute)
        {
            bool valid;

            try
            {
                valid =
                    (bool)method.Invoke(
                        target,
                        null);
            }
            catch (Exception exception)
            {
                AbeEditorGUI.HelpBox_Layout(
                    $"ValidateInput callback " +
                    $"'{method.Name}' threw an exception:\n" +
                    exception.InnerException?.Message
                    ?? exception.Message,
                    MessageType.Error);

                return;
            }

            if (!valid)
            {
                ShowMessage(
                    property,
                    attribute);
            }
        }

        private void ValidateWithParameter(
            AbeProperty property,
            MethodInfo method,
            object target,
            ParameterInfo parameter,
            ValidateInputAttribute attribute)
        {
            Type valueType =
                property.Info.ValueType;

            if (!parameter.ParameterType.IsAssignableFrom(
                    valueType))
            {
                AbeEditorGUI.HelpBox_Layout(
                    $"ValidateInput callback " +
                    $"'{method.Name}' expects " +
                    $"{parameter.ParameterType.Name}, " +
                    $"but property '{property.Name}' " +
                    $"is {valueType.Name}.",
                    MessageType.Error);

                return;
            }

            object value =
                property.ValueEntry.GetValue();

            bool valid;

            try
            {
                valid =
                    (bool)method.Invoke(
                        target,
                        new[] { value });
            }
            catch (Exception exception)
            {
                AbeEditorGUI.HelpBox_Layout(
                    $"ValidateInput callback " +
                    $"'{method.Name}' threw an exception:\n" +
                    exception.InnerException?.Message
                    ?? exception.Message,
                    MessageType.Error);

                return;
            }

            if (!valid)
            {
                ShowMessage(
                    property,
                    attribute);
            }
        }

        private void ShowMessage(
            AbeProperty property,
            ValidateInputAttribute attribute)
        {
            string message =
                string.IsNullOrEmpty(
                    attribute.Message)
                    ? $"{property.Name} is invalid."
                    : attribute.Message;

            AbeEditorGUI.HelpBox_Layout(
                message,
                MessageType.Warning);
        }
    }
}