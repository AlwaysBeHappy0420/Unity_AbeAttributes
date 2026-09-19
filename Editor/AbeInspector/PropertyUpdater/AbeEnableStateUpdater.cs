using AbeAttributes;

namespace AbeAttributes.Editor
{
    public sealed class AbeEnableStateUpdater
        : AbeStateUpdater
    {
        public override void Update(
            AbeProperty property)
        {
            EnableIfAttribute enableIf =
                property.GetAttribute<EnableIfAttribute>();

            if (enableIf != null)
            {
                property.State.SetEnabled(
                    AbeConditionUtility.Evaluate(
                        property,
                        enableIf.Conditions,
                        enableIf.ConditionOperator,
                        enableIf.Inverted,
                        enableIf.EnumValue));

                return;
            }

            DisableIfAttribute disableIf =
                property.GetAttribute<DisableIfAttribute>();

            if (disableIf != null)
            {
                property.State.SetEnabled(
                    AbeConditionUtility.Evaluate(
                        property,
                        disableIf.Conditions,
                        disableIf.ConditionOperator,
                        disableIf.Inverted,
                        disableIf.EnumValue));
            }
        }
    }
}