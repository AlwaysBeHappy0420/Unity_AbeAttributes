using System;
using System.Collections.Generic;

namespace AbeAttributes.Editor
{
    internal sealed class AbeValidatorProcessor
    {
        public void Process(
            AbeProperty property)
        {
            if (property == null)
            {
                return;
            }

            IReadOnlyList<Attribute> attributes =
                property.Attributes;

            if (attributes == null ||
                attributes.Count == 0)
            {
                return;
            }

            for (int i = 0;
                 i < attributes.Count;
                 i++)
            {
                Attribute attribute =
                    attributes[i];

                if (!(attribute
                    is AbeAttribute validatorAttribute))
                {
                    continue;
                }

                AbePropertyValidator validator =
                    validatorAttribute.GetValidator();

                if (validator == null)
                {
                    continue;
                }

                validator.Validate(
                    property);
            }
        }
    }
}