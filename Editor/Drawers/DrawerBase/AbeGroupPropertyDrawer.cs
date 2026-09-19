using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

            SavedBool expandedState =
                property.Context.GetSavedBool(
                    "group.expanded",
                    false);

            bool expanded =
                expandedState.value;

            using (new EditorGUI.DisabledScope(
                !property.State.Enabled))
            {
                EditorGUILayout.BeginVertical(
                    BoxStyle);

                Rect headerRect =
                    EditorGUILayout.GetControlRect(
                        true,
                        EditorGUIUtility.singleLineHeight);

                Rect foldoutRect =
                    new Rect(
                        headerRect.x,
                        headerRect.y,
                        headerRect.width,
                        headerRect.height);

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

                if (expanded)
                {
                    IReadOnlyList<AbeProperty> children =
                        property.Children;

                    if (children != null &&
                        children.Count > 0)
                    {
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

                EditorGUILayout.EndVertical();
            }
        }
    }
}