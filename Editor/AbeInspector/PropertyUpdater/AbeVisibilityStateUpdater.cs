using AbeAttributes;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public sealed class AbeVisibilityStateUpdater
        : AbeStateUpdater
    {
        public override void Update(
            AbeProperty property)
        {
            property.State.SetVisible(true);

            // ============================================================
            // Unity HideInInspector
            // ============================================================

            if (property.Info?.MemberInfo != null &&
                property.Info.MemberInfo.IsDefined(
                    typeof(HideInInspector),
                    true))
            {
                property.State.SetVisible(false);
            }

            // ============================================================
            // ShowIf / HideIf
            // ============================================================

            ShowIfAttributeBase showIf =
                property.GetAttribute<ShowIfAttributeBase>();

            if (showIf is ShowIfAttribute show)
            {
                property.State.SetVisible(
                    AbeConditionUtility.Evaluate(
                        property,
                        show.Conditions,
                        show.ConditionOperator,
                        show.Inverted,
                        show.EnumValue));
            }
            else if (showIf is HideIfAttribute hide)
            {
                property.State.SetVisible(
                    AbeConditionUtility.Evaluate(
                        property,
                        hide.Conditions,
                        hide.ConditionOperator,
                        hide.Inverted,
                        hide.EnumValue));
            }

            // ============================================================
            // HideIn
            // ============================================================

            HideInAttribute hideIn =
                property.GetAttribute<HideInAttribute>();

            if (hideIn != null)
            {
                bool shouldHide =
                    hideIn.Mode switch
                    {
                        HideInMode.Editor =>
                            !Application.isPlaying,

                        HideInMode.PlayMode =>
                            Application.isPlaying,

                        _ => false
                    };

                if (shouldHide)
                {
                    property.State.SetVisible(false);
                }
            }

            // ============================================================
            // GroupStart.ShowIf
            // ============================================================

            GroupStartAttribute groupStart =
                property.GetAttribute<GroupStartAttribute>();

            if (groupStart != null &&
                !string.IsNullOrEmpty(
                    groupStart.ShowIf))
            {
                property.State.SetVisible(
                    AbeConditionUtility.Evaluate(
                        property,
                        new[]
                        {
                            groupStart.ShowIf
                        },
                        EConditionOperator.And,
                        false,
                        null));
            }
        }
    }
}