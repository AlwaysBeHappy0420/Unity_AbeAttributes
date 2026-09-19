using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AbeAttributes.Editor
{
    [AttributeUsage(
        AttributeTargets.Class,
        Inherited = false)]
    public sealed class AbeProcessorPriorityAttribute
        : Attribute
    {
        public int Value { get; }

        public AbeProcessorPriorityAttribute(
            int value)
        {
            Value = value;
        }
    }

    public abstract class AbePropertyProcessor
    {
        public abstract void Process(
            AbePropertyTree tree,
            List<AbeProperty> properties);
    }

    internal static class AbePropertyProcessorLocator
    {
        public static List<AbePropertyProcessor> CreateAll()
        {
            List<Type> types =
                AbeTypeScanner
                    .FindConcreteTypes<
                        AbePropertyProcessor>();

            types =
                types
                    .OrderByDescending(
                        GetPriority)
                    .ThenBy(
                        GetTypeName,
                        StringComparer.Ordinal)
                    .ToList();

            return types
                .Select(
                    type =>
                        (AbePropertyProcessor)
                        Activator.CreateInstance(
                            type))
                .ToList();
        }

        private static int GetPriority(
            Type type)
        {
            if (type == null)
            {
                return 0;
            }

            AbeProcessorPriorityAttribute priority =
                type.GetCustomAttribute<
                    AbeProcessorPriorityAttribute>();

            return priority?.Value ?? 0;
        }

        private static string GetTypeName(
            Type type)
        {
            return type?.FullName
                   ?? string.Empty;
        }
    }
}