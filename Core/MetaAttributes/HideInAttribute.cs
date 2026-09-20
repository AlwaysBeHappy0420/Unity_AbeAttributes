using System;

namespace AbeAttributes
{
    public enum HideInMode
    {
        Editor,
        PlayMode
    }
    
    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = false)]
    public sealed class HideInAttribute
        : Attribute, IAbeAttribute
    {
        public HideInMode Mode { get; }

        public HideInAttribute(
            HideInMode mode)
        {
            Mode = mode;
        }
    }
}