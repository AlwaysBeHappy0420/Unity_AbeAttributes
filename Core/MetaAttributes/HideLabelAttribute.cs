using System;

namespace AbeAttributes
{
    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = false)]
    public sealed class HideLabelAttribute
        : AbeAttribute
    {
    }
}