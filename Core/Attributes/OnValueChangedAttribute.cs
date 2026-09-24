using System;

namespace AbeAttributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class OnValueChangedAttribute : AbeAttribute
    {
        public string CallbackName { get; private set; }

        public OnValueChangedAttribute(string callbackName)
        {
            CallbackName = callbackName;
        }
    }
}
