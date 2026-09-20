using UnityEngine;

namespace AbeAttributes.Editor
{
    [AbeDrawerPriority(90)]
    public sealed class LabelTextDrawer
        : AbeAttributeDrawer<LabelTextAttribute>
    {
        protected override void DrawAttributePropertyLayout(
            AbeProperty property,
            GUIContent label,
            LabelTextAttribute attribute)
        {
            GUIContent newLabel =
                new GUIContent(
                    attribute.Text);

            CallNextDrawer(
                property,
                newLabel);
        }
    }
}