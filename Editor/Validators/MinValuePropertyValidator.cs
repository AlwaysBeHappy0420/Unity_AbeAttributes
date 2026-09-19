using System;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public class MinValuePropertyValidator
        : AbePropertyValidator
    {
        public override void Validate(
            AbeProperty property)
        {
            MinValueAttribute attribute =
                property.GetAttribute<
                    MinValueAttribute>();

            if (attribute == null)
            {
                return;
            }

            float minValue =
                attribute.MinValue;

            if (!string.IsNullOrEmpty(
                    attribute.MinValueName))
            {
                if (!TryResolveMinValue(
                        property,
                        attribute.MinValueName,
                        out minValue))
                {
                    string warning =
                        string.Format(
                            "{0} could not resolve '{1}' " +
                            "to a numeric field, property, " +
                            "or parameterless method on {2}",
                            nameof(MinValueAttribute),
                            attribute.MinValueName,
                            property.Tree.Target
                                .GetType()
                                .Name);

                    Debug.LogWarning(
                        warning,
                        property.Tree.Target);

                    return;
                }
            }

            Type valueType =
                property.Info.ValueType;

            object value =
                property.ValueEntry.GetValue();

            if (valueType == typeof(int))
            {
                int current =
                    value != null
                        ? (int)value
                        : 0;

                if (current < minValue)
                {
                    property.ValueEntry.SetValue(
                        (int)minValue);
                }

                return;
            }

            if (valueType == typeof(float))
            {
                float current =
                    value != null
                        ? (float)value
                        : 0f;

                if (current < minValue)
                {
                    property.ValueEntry.SetValue(
                        minValue);
                }

                return;
            }

            if (valueType == typeof(Vector2))
            {
                Vector2 current =
                    value != null
                        ? (Vector2)value
                        : default;

                property.ValueEntry.SetValue(
                    Vector2.Max(
                        current,
                        new Vector2(
                            minValue,
                            minValue)));

                return;
            }

            if (valueType == typeof(Vector3))
            {
                Vector3 current =
                    value != null
                        ? (Vector3)value
                        : default;

                property.ValueEntry.SetValue(
                    Vector3.Max(
                        current,
                        new Vector3(
                            minValue,
                            minValue,
                            minValue)));

                return;
            }

            if (valueType == typeof(Vector4))
            {
                Vector4 current =
                    value != null
                        ? (Vector4)value
                        : default;

                property.ValueEntry.SetValue(
                    Vector4.Max(
                        current,
                        new Vector4(
                            minValue,
                            minValue,
                            minValue,
                            minValue)));

                return;
            }

            if (valueType == typeof(Vector2Int))
            {
                Vector2Int current =
                    value != null
                        ? (Vector2Int)value
                        : default;

                property.ValueEntry.SetValue(
                    Vector2Int.Max(
                        current,
                        new Vector2Int(
                            (int)minValue,
                            (int)minValue)));

                return;
            }

            if (valueType == typeof(Vector3Int))
            {
                Vector3Int current =
                    value != null
                        ? (Vector3Int)value
                        : default;

                property.ValueEntry.SetValue(
                    Vector3Int.Max(
                        current,
                        new Vector3Int(
                            (int)minValue,
                            (int)minValue,
                            (int)minValue)));

                return;
            }

            string unsupportedWarning =
                attribute.GetType().Name
                + " can be used only on int, float, "
                + "Vector or VectorInt fields";

            Debug.LogWarning(
                unsupportedWarning,
                property.Tree.Target);
        }

        private bool TryResolveMinValue(
            AbeProperty property,
            string name,
            out float value)
        {
            value = 0f;

            object target =
                property.Tree.Target;

            if (target == null)
            {
                return false;
            }

            var member =
                AbeReflectionUtility.FindFieldOrProperty(
                    target.GetType(),
                    name);

            if (member != null)
            {
                object raw =
                    AbeReflectionUtility.GetValue(
                        target,
                        member);

                if (TryConvertToFloat(
                        raw,
                        out value))
                {
                    return true;
                }
            }

            var method =
                AbeReflectionUtility.GetMethod(
                    target,
                    name);

            if (method == null ||
                method.GetParameters().Length != 0)
            {
                return false;
            }

            object result =
                method.Invoke(
                    target,
                    null);

            return TryConvertToFloat(
                result,
                out value);
        }

        private bool TryConvertToFloat(
            object value,
            out float result)
        {
            result = 0f;

            if (value == null)
            {
                return false;
            }

            try
            {
                result =
                    Convert.ToSingle(value);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}