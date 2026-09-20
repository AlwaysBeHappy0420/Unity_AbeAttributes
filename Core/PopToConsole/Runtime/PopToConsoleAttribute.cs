using System;

namespace AbeAttributes
{
    public enum PopToConsoleMode
    {
        Invoke,
        Get,
        Set,
        Manual
    }

    [AttributeUsage(
        AttributeTargets.Method |
        AttributeTargets.Property,
        AllowMultiple = true,
        Inherited = true)]
    public sealed class PopToConsoleAttribute
        : MetaAttribute
    {
        public PopToConsoleMode Mode { get; }

        public string BreakPoint { get; }

        public PopToConsoleAttribute(
            PopToConsoleMode mode,
            string breakPoint)
        {
            Mode = mode;
            BreakPoint = breakPoint;
        }
    }
}