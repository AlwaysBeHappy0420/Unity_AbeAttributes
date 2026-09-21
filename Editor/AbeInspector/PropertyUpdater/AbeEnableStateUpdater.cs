using AbeAttributes;

namespace AbeAttributes.Editor
{
    public sealed class AbeEnableStateUpdater
        : AbeStateUpdater
    {
        public override void Update(
            AbeProperty property)
        {
            // ============================================================
            // EnableIf
            // ============================================================

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
            }

            // ============================================================
            // DisableIf
            // ============================================================

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

            // ============================================================
            // ReadOnly
            // ============================================================

            ReadOnlyAttribute readOnly =
                property.GetAttribute<ReadOnlyAttribute>();

            if (readOnly != null)
            {
                property.State.SetEnabled(false);
            }
        }
    }
}