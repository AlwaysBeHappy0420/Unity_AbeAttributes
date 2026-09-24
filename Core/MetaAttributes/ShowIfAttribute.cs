using System;

namespace AbeAttributes
{
    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true)]
    public class ShowIfAttribute
        : ShowIfAttributeBase
    {
        public ShowIfAttribute(
            string condition)
            : base(condition)
        {
            Inverted = false;
        }

        public ShowIfAttribute(
            EConditionOperator conditionOperator,
            params string[] conditions)
            : base(
                conditionOperator,
                conditions)
        {
            Inverted = false;
        }

        public ShowIfAttribute(
            string enumName,
            object enumValue)
            : base(
                enumName,
                enumValue as Enum)
        {
            Inverted = false;
        }
    }
}