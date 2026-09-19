using System;
using AbeAttributes;

namespace AbeAttributes.Editor
{
    internal static class AbeConditionUtility
    {
        public static bool Evaluate(
            AbeProperty property,
            string[] conditions,
            EConditionOperator conditionOperator,
            bool inverted,
            Enum enumValue)
        {
            if (conditions == null ||
                conditions.Length == 0)
            {
                return true;
            }

            bool result;

            if (conditionOperator ==
                EConditionOperator.And)
            {
                result = true;

                for (int i = 0;
                     i < conditions.Length;
                     i++)
                {
                    if (!EvaluateSingle(
                            property,
                            conditions[i],
                            enumValue))
                    {
                        result = false;
                        break;
                    }
                }
            }
            else
            {
                result = false;

                for (int i = 0;
                     i < conditions.Length;
                     i++)
                {
                    if (EvaluateSingle(
                            property,
                            conditions[i],
                            enumValue))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return inverted
                ? !result
                : result;
        }

        private static bool EvaluateSingle(
            AbeProperty property,
            string conditionName,
            Enum enumValue)
        {
            object target =
                property.TargetObject;

            if (target == null ||
                string.IsNullOrEmpty(conditionName))
            {
                return false;
            }

            if (!AbeReflectionUtility.TryGetMemberValue(
                    target,
                    conditionName,
                    out object value,
                    out _))
            {
                return false;
            }

            if (enumValue != null)
            {
                return value is Enum actualEnum
                    && actualEnum.GetType() ==
                        enumValue.GetType()
                    && actualEnum.Equals(
                        enumValue);
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            return value != null;
        }
    }
}