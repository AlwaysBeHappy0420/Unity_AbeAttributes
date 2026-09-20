using System;

namespace AbeAttributes
{
    public enum PopToConsoleMode
    {
        Invoke,
        Manual
    }

    [AttributeUsage(
        AttributeTargets.Method,
        AllowMultiple = true,
        Inherited = true)]
    public sealed class PopToConsoleAttribute
        : MetaAttribute
    {
        public PopToConsoleMode Mode { get; }

        public PopToConsoleAttribute(
            PopToConsoleMode mode)
        {
            Mode = mode;
        }
    }
}