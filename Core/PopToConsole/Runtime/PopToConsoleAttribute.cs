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
        AttributeTargets.Property |
        AttributeTargets.Field,
        AllowMultiple = true,
        Inherited = true)]
    public sealed class PopToConsoleAttribute : AbeAttribute
    {
        public PopToConsoleMode Mode { get; }

        public string BreakPoint { get; set; }

        public string ValuePath { get; set; }

        public string CustomDebugMethod { get; set; }

        public PopToConsoleAttribute(
            PopToConsoleMode mode,
            string breakPoint = null)
        {
            Mode = mode;
            BreakPoint = breakPoint;
        }
    }
}
