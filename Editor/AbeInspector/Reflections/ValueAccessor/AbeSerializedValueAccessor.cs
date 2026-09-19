using System;
using UnityEditor;

namespace AbeAttributes.Editor
{
    internal sealed class AbeSerializedValueAccessor
    {
        private readonly AbeValueEntry _entry;
        private readonly AbeProperty _property;

        public AbeSerializedValueAccessor(
            AbeValueEntry entry,
            AbeProperty property)
        {
            _entry =
                entry;

            _property =
                property;
        }

        // ================================================================
        // Get
        // ================================================================

        public object GetValueForTarget(
            UnityEngine.Object target)
        {
            if (target == null)
            {
                return null;
            }

            SerializedProperty serializedProperty =
                _property.SerializedProperty;

            if (serializedProperty == null)
            {
                return null;
            }

            string propertyPath =
                serializedProperty.propertyPath;

            if (string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            // ------------------------------------------------------------
            // Single target
            // ------------------------------------------------------------

            if (_property.Tree.Targets.Length == 1 &&
                ReferenceEquals(
                    _property.Tree.Target,
                    target))
            {
                return GetSerializedValue(
                    serializedProperty);
            }

            // ------------------------------------------------------------
            // Multi target
            // ------------------------------------------------------------

            SerializedObject targetSerializedObject =
                new SerializedObject(
                    target);

            targetSerializedObject
                .UpdateIfRequiredOrScript();

            SerializedProperty targetProperty =
                targetSerializedObject
                    .FindProperty(
                        propertyPath);

            if (targetProperty == null)
            {
                return null;
            }

            return GetSerializedValue(
                targetProperty);
        }

        private object GetSerializedValue(
            SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    {
                        if (_entry.ValueType != null &&
                            _entry.ValueType.IsEnum)
                        {
                            return Enum.ToObject(
                                _entry.ValueType,
                                property.intValue);
                        }

                        return property.intValue;
                    }

                case SerializedPropertyType.Boolean:
                    return property.boolValue;

                case SerializedPropertyType.Float:
                    return property.floatValue;

                case SerializedPropertyType.String:
                    return property.stringValue;

                case SerializedPropertyType.Color:
                    return property.colorValue;

                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue;

                case SerializedPropertyType.Vector2:
                    return property.vector2Value;

                case SerializedPropertyType.Vector3:
                    return property.vector3Value;

                case SerializedPropertyType.Vector4:
                    return property.vector4Value;

                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue;

                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue;

                case SerializedPropertyType.Rect:
                    return property.rectValue;

                case SerializedPropertyType.RectInt:
                    return property.rectIntValue;

                case SerializedPropertyType.Bounds:
                    return property.boundsValue;

                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue;

                case SerializedPropertyType.Enum:
                    return Enum.ToObject(
                        _entry.ValueType,
                        property.intValue);

                default:
                    return AbeReflectionUtility.GetValue(
                        property.serializedObject
                            ?.targetObject,
                        property.propertyPath);
            }
        }

        // ================================================================
        // Set
        // ================================================================

        public void SetValue(
            object value)
        {
            SerializedProperty property =
                _property.SerializedProperty;

            if (property == null)
            {
                return;
            }

            // Unity SerializedObject / SerializedProperty
            // owns the normal serialized Undo workflow.

            SetSerializedPropertyValue(
                property,
                value);
        }

        // ================================================================
        // Set For Target
        //
        // Used by nested struct/property-chain write-back.
        // ================================================================

        public bool SetValueForTarget(
            UnityEngine.Object target,
            string propertyPath,
            object value)
        {
            if (target == null ||
                string.IsNullOrEmpty(propertyPath))
            {
                return false;
            }

            SerializedObject serializedObject =
                new SerializedObject(
                    target);

            serializedObject
                .UpdateIfRequiredOrScript();

            SerializedProperty property =
                serializedObject.FindProperty(
                    propertyPath);

            if (property == null)
            {
                return false;
            }

            SetSerializedPropertyValue(
                property,
                value);

            serializedObject
                .ApplyModifiedProperties();

            return true;
        }

        // ================================================================
        // SerializedProperty Write
        // ================================================================

        private static void SetSerializedPropertyValue(
            SerializedProperty property,
            object value)
        {
            if (property == null)
            {
                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue =
                        Convert.ToInt32(value);
                    break;

                case SerializedPropertyType.Boolean:
                    property.boolValue =
                        value != null &&
                        Convert.ToBoolean(value);
                    break;

                case SerializedPropertyType.Float:
                    property.floatValue =
                        Convert.ToSingle(value);
                    break;

                case SerializedPropertyType.String:
                    property.stringValue =
                        value as string ??
                        string.Empty;
                    break;

                case SerializedPropertyType.Color:
                    if (value is UnityEngine.Color color)
                    {
                        property.colorValue =
                            color;
                    }

                    break;

                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue =
                        value as UnityEngine.Object;
                    break;

                case SerializedPropertyType.Vector2:
                    if (value is UnityEngine.Vector2 vector2)
                    {
                        property.vector2Value =
                            vector2;
                    }

                    break;

                case SerializedPropertyType.Vector3:
                    if (value is UnityEngine.Vector3 vector3)
                    {
                        property.vector3Value =
                            vector3;
                    }

                    break;

                case SerializedPropertyType.Vector4:
                    if (value is UnityEngine.Vector4 vector4)
                    {
                        property.vector4Value =
                            vector4;
                    }

                    break;

                case SerializedPropertyType.Vector2Int:
                    if (value is UnityEngine.Vector2Int vector2Int)
                    {
                        property.vector2IntValue =
                            vector2Int;
                    }

                    break;

                case SerializedPropertyType.Vector3Int:
                    if (value is UnityEngine.Vector3Int vector3Int)
                    {
                        property.vector3IntValue =
                            vector3Int;
                    }

                    break;

                case SerializedPropertyType.Rect:
                    if (value is UnityEngine.Rect rect)
                    {
                        property.rectValue =
                            rect;
                    }

                    break;

                case SerializedPropertyType.RectInt:
                    if (value is UnityEngine.RectInt rectInt)
                    {
                        property.rectIntValue =
                            rectInt;
                    }

                    break;

                case SerializedPropertyType.Bounds:
                    if (value is UnityEngine.Bounds bounds)
                    {
                        property.boundsValue =
                            bounds;
                    }

                    break;

                case SerializedPropertyType.BoundsInt:
                    if (value is UnityEngine.BoundsInt boundsInt)
                    {
                        property.boundsIntValue =
                            boundsInt;
                    }

                    break;

                case SerializedPropertyType.Enum:
                    property.intValue =
                        Convert.ToInt32(value);
                    break;

                default:
                    SetGenericSerializedValue(
                        property,
                        value);
                    break;
            }
        }

        // ================================================================
        // Generic Serialized Value
        // ================================================================

        private static void SetGenericSerializedValue(
            SerializedProperty property,
            object value)
        {
            if (property == null)
            {
                return;
            }

            UnityEngine.Object target =
                property.serializedObject
                    ?.targetObject;

            if (target == null)
            {
                return;
            }

            AbeReflectionUtility.SetValue(
                target,
                property.propertyPath,
                value);
        }
    }
}