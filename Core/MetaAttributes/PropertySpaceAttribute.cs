using System;

namespace AbeAttributes
{
    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = false)]
    public sealed class PropertySpaceAttribute
        : AbeAttribute
    {
        public float SpaceBefore { get; }

        public float SpaceAfter { get; }

        public PropertySpaceAttribute()
            : this(4f, 4f)
        {
        }

        public PropertySpaceAttribute(
            float space)
            : this(space, space)
        {
        }

        public PropertySpaceAttribute(
            float spaceBefore,
            float spaceAfter)
        {
            SpaceBefore = Math.Max(
                0f,
                spaceBefore);

            SpaceAfter = Math.Max(
                0f,
                spaceAfter);
        }
    }
}