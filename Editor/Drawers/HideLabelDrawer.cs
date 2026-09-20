using UnityEngine;

namespace AbeAttributes.Editor
{
    [AbeDrawerPriority(100)]
    public sealed class HideLabelDrawer
        : AbeAttributeDrawer<HideLabelAttribute>
    {
        protected override void DrawAttributePropertyLayout(
            AbeProperty property,
            GUIContent label,
            HideLabelAttribute attribute)
        {
            CallNextDrawer(
                property,
                GUIContent.none);
        }
    }
}