using System;
using System.Collections.Generic;
using System.Linq;

namespace AbeAttributes.Editor
{
    public abstract class AbeStateUpdater
    {
        public abstract void Update(
            AbeProperty property);
    }

    internal static class AbeStateUpdaterLocator
    {
        public static List<AbeStateUpdater> CreateAll()
        {
            List<Type> types =
                AbeTypeScanner
                    .FindConcreteTypes<
                        AbeStateUpdater>();

            types =
                types
                    .OrderBy(
                        GetTypeName,
                        StringComparer.Ordinal)
                    .ToList();

            return types
                .Select(
                    type =>
                        (AbeStateUpdater)
                        Activator.CreateInstance(
                            type))
                .ToList();
        }

        private static string GetTypeName(
            Type type)
        {
            return type?.FullName
                   ?? string.Empty;
        }
    }
}