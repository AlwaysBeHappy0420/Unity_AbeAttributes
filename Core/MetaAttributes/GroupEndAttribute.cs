using System;

namespace AbeAttributes
{
    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = true)]
    public sealed class GroupEndAttribute : Attribute, IGroupAttribute
    {
        public string Name => string.Empty;
    }
}