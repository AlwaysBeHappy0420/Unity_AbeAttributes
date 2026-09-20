using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    [AbeDrawerPriority(80)]
    public sealed class PropertySpaceDrawer
        : AbeAttributeDrawer<PropertySpaceAttribute>
    {
        protected override void DrawAttributePropertyLayout(
            AbeProperty property,
            GUIContent label,
            PropertySpaceAttribute attribute)
        {
            if (attribute.SpaceBefore > 0f)
            {
                EditorGUILayout.Space(
                    attribute.SpaceBefore);
            }

            CallNextDrawer(
                property,
                label);

            if (attribute.SpaceAfter > 0f)
            {
                EditorGUILayout.Space(
                    attribute.SpaceAfter);
            }
        }
    }
}