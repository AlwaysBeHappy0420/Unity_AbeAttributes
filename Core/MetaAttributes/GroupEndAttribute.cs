using System;

namespace AbeAttributes
{
    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = true)]
    public sealed class GroupEndAttribute : AbeAttribute
    {
        public string Name => string.Empty;
    }
}