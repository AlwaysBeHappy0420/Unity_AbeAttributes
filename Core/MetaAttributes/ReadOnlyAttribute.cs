using System;

namespace AbeAttributes
{
    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true)]
    public class ReadOnlyAttribute : AbeAttribute
    {

    }
}
