using System;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public class MaxValuePropertyValidator
        : AbePropertyValidator
    {
        public override void Validate(
            AbeProperty property)
        {
            MaxValueAttribute attribute =
                property.GetAttribute<
                    MaxValueAttribute>();

            if (attribute == null)
            {
                return;
            }

            float maxValue =
                attribute.MaxValue;

            if (!string.IsNullOrEmpty(
                    attribute.MaxValueName))
            {
                if (!TryResolveMaxValue(
                        property,
                        attribute.MaxValueName,
                        out maxValue))
                {
                    string warning =
                        string.Format(
                            "{0} could not resolve '{1}' " +
                            "to a numeric field, property, " +
                            "or parameterless method on {2}",
                            nameof(MaxValueAttribute),
                            attribute.MaxValueName,
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

                if (current > maxValue)
                {
                    property.ValueEntry.SetValue(
                        (int)maxValue);
                }

                return;
            }

            if (valueType == typeof(float))
            {
                float current =
                    value != null
                        ? (float)value
                        : 0f;

                if (current > maxValue)
                {
                    property.ValueEntry.SetValue(
                        maxValue);
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
                    Vector2.Min(
                        current,
                        new Vector2(
                            maxValue,
                            maxValue)));

                return;
            }

            if (valueType == typeof(Vector3))
            {
                Vector3 current =
                    value != null
                        ? (Vector3)value
                        : default;

                property.ValueEntry.SetValue(
                    Vector3.Min(
                        current,
                        new Vector3(
                            maxValue,
                            maxValue,
                            maxValue)));

                return;
            }

            if (valueType == typeof(Vector4))
            {
                Vector4 current =
                    value != null
                        ? (Vector4)value
                        : default;

                property.ValueEntry.SetValue(
                    Vector4.Min(
                        current,
                        new Vector4(
                            maxValue,
                            maxValue,
                            maxValue,
                            maxValue)));

                return;
            }

            if (valueType == typeof(Vector2Int))
            {
                Vector2Int current =
                    value != null
                        ? (Vector2Int)value
                        : default;

                property.ValueEntry.SetValue(
                    Vector2Int.Min(
                        current,
                        new Vector2Int(
                            (int)maxValue,
                            (int)maxValue)));

                return;
            }

            if (valueType == typeof(Vector3Int))
            {
                Vector3Int current =
                    value != null
                        ? (Vector3Int)value
                        : default;

                property.ValueEntry.SetValue(
                    Vector3Int.Min(
                        current,
                        new Vector3Int(
                            (int)maxValue,
                            (int)maxValue,
                            (int)maxValue)));

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

        private bool TryResolveMaxValue(
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