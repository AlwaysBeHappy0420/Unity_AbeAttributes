using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    internal sealed class AbeCollectionDrawer
        : AbeValueDrawer<IEnumerable>
    {
        private const float BoxRightSpace = 0f;

        private static GUIStyle boxStyle;

        private static GUIStyle BoxStyle
        {
            get
            {
                if (boxStyle == null ||
                    boxStyle.normal.background == null)
                {
                    boxStyle =
                        new GUIStyle(
                            EditorStyles.helpBox);

                    boxStyle.margin =
                        new RectOffset(
                            boxStyle.margin.left,
                            (int)BoxRightSpace,
                            boxStyle.margin.top,
                            boxStyle.margin.bottom);

                    boxStyle.padding =
                        new RectOffset(
                            boxStyle.padding.left,
                            (int)BoxRightSpace,
                            boxStyle.padding.top,
                            boxStyle.padding.bottom);
                }

                return boxStyle;
            }
        }

        protected override void DrawValuePropertyLayout(
    AbeProperty property,
    GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            if (!AbeCollectionUtility.IsCollectionType(
                    property.ValueEntry.ValueType))
            {
                CallNextDrawer(
                    property,
                    label);

                return;
            }

            SerializedProperty serializedProperty =
                property.SerializedProperty;

            if (AbeCollectionUtility
                .IsSerializedCollection(
                    serializedProperty))
            {
                DrawSerializedCollection(
                    property,
                    serializedProperty,
                    label);

                return;
            }

            DrawNativeCollection(
                property,
                label);
        }

        // ================================================================
        // Serialized Collection
        // ================================================================

        private void DrawSerializedCollection(
            AbeProperty property,
            SerializedProperty serializedProperty,
            GUIContent label)
        {
            int listDepth =
                GetListDepth(property);

            using (new EditorGUI.DisabledScope(
                !property.State.Enabled))
            {
                DrawCollectionBackground(
                    listDepth);

                // ========================================================
                // Header
                // ========================================================

                Rect headerRect =
                    EditorGUI.IndentedRect(
                        EditorGUILayout.GetControlRect(
                            true,
                            EditorGUIUtility.singleLineHeight));

                int oldIndent =
                    EditorGUI.indentLevel;

                EditorGUI.indentLevel = 0;

                const float leftPadding = 10f;
                const float sizeWidth = 30f;
                const float sizeSpacing = 4f;
                const float sizeRightInset = 5f;

                Rect sizeRect =
                    new Rect(
                        headerRect.xMax
                        - sizeWidth
                        - sizeRightInset,
                        headerRect.y,
                        sizeWidth,
                        headerRect.height);

                float foldoutX =
                    headerRect.x
                    + leftPadding;

                Rect foldoutRect =
                    new Rect(
                        foldoutX,
                        headerRect.y,
                        Mathf.Max(
                            0f,
                            sizeRect.x
                            - sizeSpacing
                            - foldoutX),
                        headerRect.height);

                serializedProperty.isExpanded =
                    EditorGUI.Foldout(
                        foldoutRect,
                        serializedProperty.isExpanded,
                        label,
                        true);

                int oldSize =
                    serializedProperty.arraySize;

                int newSize =
                    Mathf.Max(
                        0,
                        EditorGUI.IntField(
                            sizeRect,
                            oldSize));

                EditorGUI.indentLevel =
                    oldIndent;

                if (newSize != oldSize)
                {
                    serializedProperty.arraySize =
                        newSize;

                    property.ClearChildren();
                }

                // ========================================================
                // Elements
                // ========================================================

                if (serializedProperty.isExpanded)
                {
                    IReadOnlyList<AbeProperty> children =
                        property.Children;

                    for (int i = 0;
                         i < children.Count;
                         i++)
                    {
                        AbeProperty child =
                            children[i];

                        DrawElement(
                            child,
                            i,
                            listDepth,
                            IsCollectionProperty(
                                child));
                    }

                    DrawSerializedCollectionButtons(
                        property,
                        serializedProperty);
                }

                EditorGUILayout.EndVertical();
            }
        }

        // ================================================================
        // Native Collection
        // ================================================================

        private void DrawNativeCollection(
            AbeProperty property,
            GUIContent label)
        {
            int listDepth =
                GetListDepth(property);

            using (new EditorGUI.DisabledScope(
                !property.State.Enabled))
            {
                DrawCollectionBackground(
                    listDepth);

                // ========================================================
                // Header
                // ========================================================

                Rect headerRect =
                    EditorGUI.IndentedRect(
                        EditorGUILayout.GetControlRect(
                            true,
                            EditorGUIUtility.singleLineHeight));

                int oldIndent =
                    EditorGUI.indentLevel;

                EditorGUI.indentLevel = 0;

                const float leftPadding = 10f;
                const float sizeWidth = 30f;
                const float sizeSpacing = 4f;
                const float sizeRightInset = 5f;

                Rect sizeRect =
                    new Rect(
                        headerRect.xMax
                        - sizeWidth
                        - sizeRightInset,
                        headerRect.y,
                        sizeWidth,
                        headerRect.height);

                float foldoutX =
                    headerRect.x
                    + leftPadding;

                Rect foldoutRect =
                    new Rect(
                        foldoutX,
                        headerRect.y,
                        Mathf.Max(
                            0f,
                            sizeRect.x
                            - sizeSpacing
                            - foldoutX),
                        headerRect.height);

                SavedBool expandedState =
                    property.Context.GetSavedBool(
                        "nativeCollection.expanded",
                        false);

                bool expanded =
                    expandedState.value;

                bool newExpanded =
                    EditorGUI.Foldout(
                        foldoutRect,
                        expanded,
                        label,
                        true);

                if (newExpanded != expanded)
                {
                    expandedState.value =
                        newExpanded;

                    expanded =
                        newExpanded;
                }

                bool mixedSize;

                int oldSize =
                    property.ValueEntry
                        .GetCollectionCount(
                            out mixedSize);

                bool canResize =
                    property.ValueEntry
                        .CanResizeCollection();

                int newSize;

                bool oldMixedValue =
                    EditorGUI.showMixedValue;

                EditorGUI.showMixedValue =
                    mixedSize;

                using (new EditorGUI.DisabledScope(
                    !canResize))
                {
                    newSize =
                        Mathf.Max(
                            0,
                            EditorGUI.IntField(
                                sizeRect,
                                oldSize));
                }

                EditorGUI.showMixedValue =
                    oldMixedValue;

                EditorGUI.indentLevel =
                    oldIndent;

                if (canResize &&
                    newSize != oldSize)
                {
                    if (property.ValueEntry
                        .TrySetCollectionSize(
                            newSize))
                    {
                        property.ClearChildren();
                    }
                }

                // ========================================================
                // Elements
                // ========================================================

                if (expanded)
                {
                    IReadOnlyList<AbeProperty> children =
                        property.Children;

                    for (int i = 0;
                         i < children.Count;
                         i++)
                    {
                        AbeProperty child =
                            children[i];

                        DrawElement(
                            child,
                            i,
                            listDepth,
                            IsCollectionProperty(
                                child));
                    }

                    DrawNativeCollectionButtons(
                        property,
                        oldSize,
                        canResize);
                }

                EditorGUILayout.EndVertical();
            }
        }

        // ================================================================
        // Element
        // ================================================================

        private void DrawElement(
            AbeProperty child,
            int index,
            int listDepth,
            bool childIsCollection)
        {
            if (child == null)
            {
                return;
            }

            Color oldElementColor =
                GUI.backgroundColor;

            GUI.backgroundColor =
                GetListElementBackgroundColor(
                    listDepth);

            EditorGUILayout.BeginVertical(
                BoxStyle);

            GUI.backgroundColor =
                oldElementColor;

            if (!childIsCollection)
            {
                EditorGUI.indentLevel++;
            }

            child.Draw(
                new GUIContent(
                    $"Element {index}"));

            if (!childIsCollection)
            {
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        // ================================================================
        // Serialized Buttons
        // ================================================================

        private void DrawSerializedCollectionButtons(
            AbeProperty property,
            SerializedProperty serializedProperty)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(
                serializedProperty.arraySize <= 0))
            {
                if (GUILayout.Button(
                    "-",
                    EditorStyles.miniButtonLeft,
                    GUILayout.Width(24f)))
                {
                    int index =
                        serializedProperty.arraySize - 1;

                    serializedProperty
                        .DeleteArrayElementAtIndex(
                            index);

                    property.ClearChildren();
                }
            }

            if (GUILayout.Button(
                "+",
                EditorStyles.miniButtonRight,
                GUILayout.Width(24f)))
            {
                int index =
                    serializedProperty.arraySize;

                serializedProperty
                    .InsertArrayElementAtIndex(
                        index);

                property.ClearChildren();
            }

            EditorGUILayout.EndHorizontal();
        }

        // ================================================================
        // Native Buttons
        // ================================================================

        private void DrawNativeCollectionButtons(
            AbeProperty property,
            int oldSize,
            bool canResize)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(
                !canResize ||
                oldSize <= 0))
            {
                if (GUILayout.Button(
                    "-",
                    EditorStyles.miniButtonLeft,
                    GUILayout.Width(24f)))
                {
                    if (property.ValueEntry
                        .TrySetCollectionSize(
                            oldSize - 1))
                    {
                        property.ClearChildren();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(
                !canResize))
            {
                if (GUILayout.Button(
                    "+",
                    EditorStyles.miniButtonRight,
                    GUILayout.Width(24f)))
                {
                    if (property.ValueEntry
                        .TrySetCollectionSize(
                            oldSize + 1))
                    {
                        property.ClearChildren();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // ================================================================
        // Collection Background
        // ================================================================

        private void DrawCollectionBackground(
            int depth)
        {
            Color oldColor =
                GUI.backgroundColor;

            GUI.backgroundColor =
                GetListBackgroundColor(
                    depth);

            EditorGUILayout.BeginVertical(
                BoxStyle);

            GUI.backgroundColor =
                oldColor;
        }

        // ================================================================
        // Collection Detection
        // ================================================================

        private static bool IsCollectionProperty(
            AbeProperty property)
        {
            if (property == null)
            {
                return false;
            }

            if (property.SerializedProperty != null)
            {
                return AbeCollectionUtility
                    .IsSerializedCollection(
                        property.SerializedProperty);
            }

            return AbeCollectionUtility
                .IsCollectionType(
                    property.ValueEntry.ValueType);
        }

        // ================================================================
        // List Depth
        // ================================================================

        private int GetListDepth(
            AbeProperty property)
        {
            int depth = 0;

            AbeProperty current =
                property.Parent;

            while (current != null)
            {
                if (IsCollectionProperty(
                    current))
                {
                    depth++;
                }

                current =
                    current.Parent;
            }

            return depth;
        }

        // ================================================================
        // List Colors
        // ================================================================

        private Color GetListBackgroundColor(
            int depth)
        {
            Color baseColor =
                GUI.backgroundColor;

            float factor =
                Mathf.Max(
                    0.25f,
                    Mathf.Pow(
                        0.5f,
                        depth + 1));

            return new Color(
                baseColor.r * factor,
                baseColor.g * factor,
                baseColor.b * factor,
                baseColor.a);
        }

        private Color GetListElementBackgroundColor(
            int depth)
        {
            Color baseColor =
                GUI.backgroundColor;

            float factor =
                Mathf.Max(
                    0.4f,
                    Mathf.Pow(
                        0.65f,
                        depth + 1));

            return new Color(
                baseColor.r * factor,
                baseColor.g * factor,
                baseColor.b * factor,
                baseColor.a);
        }
    }
}