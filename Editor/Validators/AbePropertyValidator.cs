using System;
using System.Collections.Generic;

namespace AbeAttributes.Editor
{
    public abstract class AbePropertyValidator
    {
        public abstract void Validate(
            AbeProperty property);
    }

    public static class ValidatorAttributeExtensions
    {
        private static readonly Dictionary<
            Type,
            AbePropertyValidator>
            ValidatorsByAttributeType =
                new Dictionary<
                    Type,
                    AbePropertyValidator>
                {
                    {
                        typeof(MinValueAttribute),
                        new MinValuePropertyValidator()
                    },
                    {
                        typeof(MaxValueAttribute),
                        new MaxValuePropertyValidator()
                    },
                    {
                        typeof(RequiredAttribute),
                        new RequiredPropertyValidator()
                    },
                    {
                        typeof(ValidateInputAttribute),
                        new ValidateInputPropertyValidator()
                    }
                };

        public static AbePropertyValidator GetValidator(
            this ValidatorAttribute attribute)
        {
            if (attribute == null)
            {
                return null;
            }

            ValidatorsByAttributeType.TryGetValue(
                attribute.GetType(),
                out AbePropertyValidator validator);

            return validator;
        }
    }
}