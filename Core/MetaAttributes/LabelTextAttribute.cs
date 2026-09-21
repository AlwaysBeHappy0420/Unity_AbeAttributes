using System;

namespace AbeAttributes
{
    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = false)]
    public sealed class LabelTextAttribute
        : AbeAttribute
    {
        public string Text { get; }

        public LabelTextAttribute(
            string text)
        {
            Text = text ?? string.Empty;
        }
    }
}