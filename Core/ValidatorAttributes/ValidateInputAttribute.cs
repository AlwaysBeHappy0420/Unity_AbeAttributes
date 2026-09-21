using System;

namespace AbeAttributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ValidateInputAttribute : AbeAttribute
    {
        public string CallbackName { get; private set; }
        public string Message { get; private set; }

        public ValidateInputAttribute(string callbackName, string message = null)
        {
            CallbackName = callbackName;
            Message = message;
        }
    }
}
