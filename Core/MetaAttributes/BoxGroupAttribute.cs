using System;

namespace AbeAttributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class BoxGroupAttribute : Attribute, IGroupAttribute
    {
        public string Name { get; }

        public BoxGroupAttribute(string name)
        {
            Name = name;
        }
    }
}
