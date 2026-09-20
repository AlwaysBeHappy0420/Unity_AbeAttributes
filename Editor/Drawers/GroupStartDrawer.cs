using UnityEngine;

namespace AbeAttributes.Editor
{
    // ====================================================================
    // Box
    // ====================================================================

    [AbeDrawerPriority(80)]
    public sealed class GroupStartBoxDrawer
        : AbeGroupDrawer<GroupStartAttribute>
    {
        protected override void DrawGroupPropertyLayout(
            AbeProperty property,
            GUIContent label,
            GroupStartAttribute attribute)
        {
            if (attribute.Style != GroupStyle.Box)
            {
                CallNextDrawer(
                    property,
                    label);

                return;
            }

            AbeGroupDrawerUtility.AbeBoxGroupDrawerUtility.Draw(
                property,
                attribute.Name);
        }
    }

    // ====================================================================
    // Vertical
    // ====================================================================

    [AbeDrawerPriority(80)]
    public sealed class GroupStartVerticalDrawer
        : AbeGroupDrawer<GroupStartAttribute>
    {
        protected override void DrawGroupPropertyLayout(
            AbeProperty property,
            GUIContent label,
            GroupStartAttribute attribute)
        {
            if (attribute.Style != GroupStyle.Vertical)
            {
                CallNextDrawer(
                    property,
                    label);

                return;
            }

            AbeGroupDrawerUtility.AbeVerticalGroupDrawerUtility.Draw(
                property,
                attribute.Name);
        }
    }

    // ====================================================================
    // Horizontal
    // ====================================================================

    [AbeDrawerPriority(80)]
    public sealed class GroupStartHorizontalDrawer
        : AbeGroupDrawer<GroupStartAttribute>
    {
        protected override void DrawGroupPropertyLayout(
            AbeProperty property,
            GUIContent label,
            GroupStartAttribute attribute)
        {
            if (attribute.Style != GroupStyle.Horizontal)
            {
                CallNextDrawer(
                    property,
                    label);

                return;
            }

            AbeGroupDrawerUtility.AbeHorizontalGroupDrawerUtility.Draw(
                property,
                attribute.Name);
        }
    }


}