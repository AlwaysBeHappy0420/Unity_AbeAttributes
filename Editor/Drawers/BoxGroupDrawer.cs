using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    [AbeDrawerPriority(80)]
    public sealed class BoxGroupDrawer
        : AbeGroupDrawer<BoxGroupAttribute>
    {
        protected override void DrawGroupPropertyLayout(
            AbeProperty property,
            GUIContent label,
            BoxGroupAttribute attribute)
        {
            AbeEditorGUI.BeginBoxGroup_Layout(
                attribute.Name);

            DrawChildren(property);

            AbeEditorGUI.EndBoxGroup_Layout();
        }
    }
}