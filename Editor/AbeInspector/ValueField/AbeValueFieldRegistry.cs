using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    internal static class AbeValueFieldRegistry
    {
        private sealed class ValueFieldHandler
        {
            public readonly Func<Type, bool> CanHandle;
            public readonly Func<GUIContent, object, Type, object> Draw;

            public ValueFieldHandler(
                Func<Type, bool> canHandle,
                Func<GUIContent, object, Type, object> draw)
            {
                CanHandle = canHandle;
                Draw = draw;
            }
        }

        private static readonly List<ValueFieldHandler> Handlers =
            new List<ValueFieldHandler>();

        static AbeValueFieldRegistry()
        {
            RegisterBuiltInTypes();
        }

        // ====================================================================
        // Public API
        // ====================================================================

        public static bool CanDraw(Type type)
        {
            type = NormalizeType(type);

            if (type == null)
            {
                return false;
            }

            for (int i = 0; i < Handlers.Count; i++)
            {
                if (Handlers[i].CanHandle(type))
                {
                    return true;
                }
            }

            return false;
        }

        public static object Draw(
            GUIContent label,
            object value,
            Type type)
        {
            type = NormalizeType(type);

            if (type == null)
            {
                return value;
            }

            for (int i = 0; i < Handlers.Count; i++)
            {
                ValueFieldHandler handler =
                    Handlers[i];

                if (!handler.CanHandle(type))
                {
                    continue;
                }

                return handler.Draw(
                    label,
                    value,
                    type);
            }

            return value;
        }

        // ====================================================================
        // Registration
        // ====================================================================

        private static void RegisterBuiltInTypes()
        {
            // ================================================================
            // Primitive
            // ================================================================

            RegisterExact(
                typeof(bool),
                DrawBool);

            RegisterExact(
                typeof(string),
                DrawString);

            RegisterExact(
                typeof(int),
                DrawInt);

            RegisterExact(
                typeof(long),
                DrawLong);

            RegisterExact(
                typeof(float),
                DrawFloat);

            RegisterExact(
                typeof(double),
                DrawDouble);

            RegisterExact(
                typeof(short),
                DrawShort);

            RegisterExact(
                typeof(ushort),
                DrawUShort);

            RegisterExact(
                typeof(byte),
                DrawByte);

            RegisterExact(
                typeof(sbyte),
                DrawSByte);

            RegisterExact(
                typeof(uint),
                DrawUInt);

            RegisterExact(
                typeof(ulong),
                DrawULong);

            // ================================================================
            // Unity Structs
            // ================================================================

            RegisterExact(
                typeof(Vector2),
                DrawVector2);

            RegisterExact(
                typeof(Vector2Int),
                DrawVector2Int);

            RegisterExact(
                typeof(Vector3),
                DrawVector3);

            RegisterExact(
                typeof(Vector3Int),
                DrawVector3Int);

            RegisterExact(
                typeof(Vector4),
                DrawVector4);

            RegisterExact(
                typeof(Color),
                DrawColor);

            RegisterExact(
                typeof(Rect),
                DrawRect);

            RegisterExact(
                typeof(RectInt),
                DrawRectInt);

            RegisterExact(
                typeof(Bounds),
                DrawBounds);

            RegisterExact(
                typeof(BoundsInt),
                DrawBoundsInt);

            // ================================================================
            // Enum
            // ================================================================

            Register(
                type => type.IsEnum,
                DrawEnum);

            // ================================================================
            // Unity Object
            // ================================================================

            Register(
                type =>
                    typeof(UnityEngine.Object)
                        .IsAssignableFrom(type),
                DrawUnityObject);
        }

        private static void RegisterExact(
            Type type,
            Func<GUIContent, object, Type, object> draw)
        {
            Register(
                registeredType => registeredType == type,
                draw);
        }

        private static void Register(
            Func<Type, bool> canHandle,
            Func<GUIContent, object, Type, object> draw)
        {
            if (canHandle == null)
            {
                throw new ArgumentNullException(
                    nameof(canHandle));
            }

            if (draw == null)
            {
                throw new ArgumentNullException(
                    nameof(draw));
            }

            Handlers.Add(
                new ValueFieldHandler(
                    canHandle,
                    draw));
        }

        // ====================================================================
        // Primitive Drawers
        // ====================================================================

        private static object DrawBool(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.Toggle(
                label,
                value is bool boolValue &&
                boolValue);
        }

        private static object DrawString(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.TextField(
                label,
                value as string ??
                string.Empty);
        }

        private static object DrawInt(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.IntField(
                label,
                value != null
                    ? (int)value
                    : 0);
        }

        private static object DrawLong(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.LongField(
                label,
                value != null
                    ? (long)value
                    : 0L);
        }

        private static object DrawFloat(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.FloatField(
                label,
                value != null
                    ? (float)value
                    : 0f);
        }

        private static object DrawDouble(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.DoubleField(
                label,
                value != null
                    ? (double)value
                    : 0d);
        }

        private static object DrawShort(
            GUIContent label,
            object value,
            Type type)
        {
            int current =
                value != null
                    ? (short)value
                    : 0;

            int result =
                EditorGUILayout.IntField(
                    label,
                    current);

            return (short)Mathf.Clamp(
                result,
                short.MinValue,
                short.MaxValue);
        }

        private static object DrawUShort(
            GUIContent label,
            object value,
            Type type)
        {
            int current =
                value != null
                    ? (ushort)value
                    : 0;

            int result =
                EditorGUILayout.IntField(
                    label,
                    current);

            return (ushort)Mathf.Clamp(
                result,
                ushort.MinValue,
                ushort.MaxValue);
        }

        private static object DrawByte(
            GUIContent label,
            object value,
            Type type)
        {
            int current =
                value != null
                    ? (byte)value
                    : 0;

            int result =
                EditorGUILayout.IntField(
                    label,
                    current);

            return (byte)Mathf.Clamp(
                result,
                byte.MinValue,
                byte.MaxValue);
        }

        private static object DrawSByte(
            GUIContent label,
            object value,
            Type type)
        {
            int current =
                value != null
                    ? (sbyte)value
                    : 0;

            int result =
                EditorGUILayout.IntField(
                    label,
                    current);

            return (sbyte)Mathf.Clamp(
                result,
                sbyte.MinValue,
                sbyte.MaxValue);
        }

        private static object DrawUInt(
            GUIContent label,
            object value,
            Type type)
        {
            uint current =
                value != null
                    ? (uint)value
                    : 0u;

            long result =
                EditorGUILayout.LongField(
                    label,
                    current);

            if (result < 0)
            {
                result = 0;
            }

            if ((ulong)result > uint.MaxValue)
            {
                result =
                    uint.MaxValue;
            }

            return (uint)result;
        }

        private static object DrawULong(
            GUIContent label,
            object value,
            Type type)
        {
            ulong current =
                value != null
                    ? (ulong)value
                    : 0UL;

            long displayValue =
                current > long.MaxValue
                    ? long.MaxValue
                    : (long)current;

            long result =
                EditorGUILayout.LongField(
                    label,
                    displayValue);

            return result < 0
                ? 0UL
                : (ulong)result;
        }

        // ====================================================================
        // Unity Struct Drawers
        // ====================================================================

        private static object DrawVector2(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.Vector2Field(
                label,
                value != null
                    ? (Vector2)value
                    : default);
        }

        private static object DrawVector2Int(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.Vector2IntField(
                label,
                value != null
                    ? (Vector2Int)value
                    : default);
        }

        private static object DrawVector3(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.Vector3Field(
                label,
                value != null
                    ? (Vector3)value
                    : default);
        }

        private static object DrawVector3Int(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.Vector3IntField(
                label,
                value != null
                    ? (Vector3Int)value
                    : default);
        }

        private static object DrawVector4(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.Vector4Field(
                label,
                value != null
                    ? (Vector4)value
                    : default);
        }

        private static object DrawColor(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.ColorField(
                label,
                value != null
                    ? (Color)value
                    : Color.white);
        }

        private static object DrawRect(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.RectField(
                label,
                value != null
                    ? (Rect)value
                    : default);
        }

        private static object DrawRectInt(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.RectIntField(
                label,
                value != null
                    ? (RectInt)value
                    : default);
        }

        private static object DrawBounds(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.BoundsField(
                label,
                value != null
                    ? (Bounds)value
                    : default);
        }

        private static object DrawBoundsInt(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.BoundsIntField(
                label,
                value != null
                    ? (BoundsInt)value
                    : default);
        }

        // ====================================================================
        // Enum
        // ====================================================================

        private static object DrawEnum(
            GUIContent label,
            object value,
            Type type)
        {
            Enum enumValue;

            if (value is Enum currentEnum)
            {
                enumValue =
                    currentEnum;
            }
            else
            {
                Array enumValues =
                    Enum.GetValues(type);

                enumValue =
                    enumValues.Length > 0
                        ? (Enum)enumValues.GetValue(0)
                        : null;
            }

            if (enumValue == null)
            {
                EditorGUILayout.LabelField(
                    label,
                    "null");

                return value;
            }

            return EditorGUILayout.EnumPopup(
                label,
                enumValue);
        }

        // ====================================================================
        // Unity Object
        // ====================================================================

        private static object DrawUnityObject(
            GUIContent label,
            object value,
            Type type)
        {
            return EditorGUILayout.ObjectField(
                label,
                value as UnityEngine.Object,
                type,
                true);
        }

        // ====================================================================
        // Type Normalization
        // ====================================================================

        private static Type NormalizeType(
            Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (type.IsByRef)
            {
                type =
                    type.GetElementType();
            }

            return type;
        }
    }
}