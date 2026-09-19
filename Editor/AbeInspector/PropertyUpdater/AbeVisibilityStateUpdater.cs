using AbeAttributes;

namespace AbeAttributes.Editor
{
    public sealed class AbeVisibilityStateUpdater
        : AbeStateUpdater
    {
        public override void Update(
            AbeProperty property)
        {
            property.State.SetVisible(true);

            ShowIfAttribute showIf =
                property.GetAttribute<ShowIfAttribute>();

            if (showIf != null)
            {
                property.State.SetVisible(
                    AbeConditionUtility.Evaluate(
                        property,
                        showIf.Conditions,
                        showIf.ConditionOperator,
                        showIf.Inverted,
                        showIf.EnumValue));

                return;
            }

            HideIfAttribute hideIf =
                property.GetAttribute<HideIfAttribute>();

            if (hideIf != null)
            {
                property.State.SetVisible(
                    AbeConditionUtility.Evaluate(
                        property,
                        hideIf.Conditions,
                        hideIf.ConditionOperator,
                        hideIf.Inverted,
                        hideIf.EnumValue));

                return;
            }
        }
    }
}