using AbeAttributes.Editor;
using UnityEditor;

namespace AbeAttributes
{
    public class AbeGroupDrawerUtility
    {
        // ====================================================================
        // Box Utility
        // ====================================================================

        internal static class AbeBoxGroupDrawerUtility
        {
            public static void Draw(
                AbeProperty property,
                string name)
            {
                AbeEditorGUI.BeginBoxGroup_Layout(
                    name);

                DrawChildren(
                    property);

                AbeEditorGUI.EndBoxGroup_Layout();
            }

            private static void DrawChildren(
                AbeProperty property)
            {
                if (property == null)
                {
                    return;
                }

                foreach (AbeProperty child
                         in property.Children)
                {
                    if (child == null)
                    {
                        continue;
                    }

                    child.Draw();
                }
            }
        }

        // ====================================================================
        // Vertical Utility
        // ====================================================================

        internal static class AbeVerticalGroupDrawerUtility
        {
            public static void Draw(
                AbeProperty property,
                string name)
            {
                EditorGUILayout.BeginVertical();

                if (!string.IsNullOrEmpty(name))
                {
                    EditorGUILayout.LabelField(
                        name,
                        EditorStyles.boldLabel);
                }

                DrawChildren(
                    property);

                EditorGUILayout.EndVertical();
            }

            private static void DrawChildren(
                AbeProperty property)
            {
                if (property == null)
                {
                    return;
                }

                foreach (AbeProperty child
                         in property.Children)
                {
                    if (child == null)
                    {
                        continue;
                    }

                    child.Draw();
                }
            }
        }

        // ====================================================================
        // Horizontal Utility
        // ====================================================================

        internal static class AbeHorizontalGroupDrawerUtility
        {
            public static void Draw(
                AbeProperty property,
                string name)
            {
                EditorGUILayout.BeginVertical();

                if (!string.IsNullOrEmpty(name))
                {
                    EditorGUILayout.LabelField(
                        name,
                        EditorStyles.boldLabel);
                }

                EditorGUILayout.BeginHorizontal();

                DrawChildren(
                    property);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            private static void DrawChildren(
                AbeProperty property)
            {
                if (property == null)
                {
                    return;
                }

                foreach (AbeProperty child
                         in property.Children)
                {
                    if (child == null)
                    {
                        continue;
                    }

                    child.Draw();
                }
            }
        }
    }
}
