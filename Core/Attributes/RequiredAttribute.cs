using System;
using UnityEngine;

namespace AbeAttributes
{
    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property,
        AllowMultiple = false)]
    public sealed class RequiredAttribute : AbeAttribute
    {
        public string Message { get; }

        public Type[] RequiredTypes { get; }

        public bool HasRequiredTypes =>
            RequiredTypes != null &&
            RequiredTypes.Length > 0;

        // ================================================================
        // [Required]
        // ================================================================

        public RequiredAttribute()
        {
            RequiredTypes = Array.Empty<Type>();
        }

        // ================================================================
        // [Required("Custom message")]
        // ================================================================

        public RequiredAttribute(
            string message)
        {
            Message = message;
            RequiredTypes = Array.Empty<Type>();
        }

        // ================================================================
        // [Required(typeof(Rigidbody))]
        // ================================================================

        public RequiredAttribute(
            Type requiredType)
        {
            RequiredTypes =
                new[]
                {
                    requiredType
                };
        }

        // ================================================================
        // [Required(typeof(Rigidbody), typeof(Collider))]
        // ================================================================

        public RequiredAttribute(
            params Type[] requiredTypes)
        {
            RequiredTypes =
                requiredTypes ??
                Array.Empty<Type>();
        }
    }
}