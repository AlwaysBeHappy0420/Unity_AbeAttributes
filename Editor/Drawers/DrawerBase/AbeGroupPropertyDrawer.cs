using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AbeAttributes;
namespace AbeAttributes.Editor
{
    [AbePropertyKindDrawer(
        AbePropertyKind.Group)]
    internal sealed class AbeGroupPropertyDrawer
        : AbePropertyKindDrawer
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

        internal override AbePropertyKind PropertyKind =>
            AbePropertyKind.Group;

        protected override void DrawPropertyKindLayout(
            AbeProperty property,
            GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            GroupStartAttribute attribute =
                property.GetAttribute<
                    GroupStartAttribute>();

            if (attribute == null)
            {
                return;
            }

            SavedBool expandedState =
                property.Context.GetSavedBool(
                    "group.expanded",
                    false);

            bool expanded =
                expandedState.value;

            using (new EditorGUI.DisabledScope(
                !property.State.Enabled))
            {
                DrawGroup(
                    property,
                    label,
                    attribute,
                    expanded,
                    expandedState);
            }
        }

        private static void DrawGroup(
            AbeProperty property,
            GUIContent label,
            GroupStartAttribute attribute,
            bool expanded,
            SavedBool expandedState)
        {
            switch (attribute.Style)
            {
                case GroupStyle.Horizontal:

                    DrawHorizontalGroup(
                        property,
                        label,
                        expanded,
                        expandedState);

                    break;

                case GroupStyle.Vertical:

                    DrawVerticalGroup(
                        property,
                        label,
                        expanded,
                        expandedState);

                    break;

                case GroupStyle.Box:

                default:

                    DrawBoxGroup(
                        property,
                        label,
                        expanded,
                        expandedState);

                    break;
            }
        }

        // ================================================================
        // Box
        // ================================================================

        private static void DrawBoxGroup(
            AbeProperty property,
            GUIContent label,
            bool expanded,
            SavedBool expandedState)
        {
            EditorGUILayout.BeginVertical(
                BoxStyle);

            DrawHeader(
                label,
                expanded,
                expandedState);

            if (expanded)
            {
                DrawChildren(
                    property);
            }

            EditorGUILayout.EndVertical();
        }

        // ================================================================
        // Vertical
        // ================================================================

        private static void DrawVerticalGroup(
            AbeProperty property,
            GUIContent label,
            bool expanded,
            SavedBool expandedState)
        {
            EditorGUILayout.BeginVertical();

            DrawHeader(
                label,
                expanded,
                expandedState);

            if (expanded)
            {
                DrawChildren(
                    property);
            }

            EditorGUILayout.EndVertical();
        }

        // ================================================================
        // Horizontal
        // ================================================================

        private static void DrawHorizontalGroup(
            AbeProperty property,
            GUIContent label,
            bool expanded,
            SavedBool expandedState)
        {
            EditorGUILayout.BeginVertical();

            DrawHeader(
                label,
                expanded,
                expandedState);

            if (expanded)
            {
                IReadOnlyList<AbeProperty> children =
                    property.Children;

                if (children != null &&
                    children.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();

                    for (int i = 0;
                         i < children.Count;
                         i++)
                    {
                        AbeProperty child =
                            children[i];

                        if (child == null)
                        {
                            continue;
                        }

                        EditorGUILayout.BeginVertical();

                        child.Draw();

                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ================================================================
        // Header
        // ================================================================

        private static void DrawHeader(
            GUIContent label,
            bool expanded,
            SavedBool expandedState)
        {
            Rect headerRect =
                EditorGUILayout.GetControlRect(
                    true,
                    EditorGUIUtility.singleLineHeight);

            bool newExpanded =
                EditorGUI.Foldout(
                    headerRect,
                    expanded,
                    label,
                    true);

            if (newExpanded != expanded)
            {
                expandedState.value =
                    newExpanded;
            }
        }

        // ================================================================
        // Children
        // ================================================================

        private static void DrawChildren(
            AbeProperty group)
        {
            IReadOnlyList<AbeProperty> children =
                group.Children;

            if (children == null ||
                children.Count == 0)
            {
                return;
            }

            EditorGUI.indentLevel++;

            for (int i = 0;
                 i < children.Count;
                 i++)
            {
                AbeProperty child =
                    children[i];

                if (child == null)
                {
                    continue;
                }

                child.Draw();
            }

            EditorGUI.indentLevel--;
        }
    }
}