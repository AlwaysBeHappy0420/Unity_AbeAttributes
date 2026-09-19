using System;
using System.Collections.Generic;
using UnityEditor;

namespace AbeAttributes.Editor
{
    internal static class AbeTypeScanner
    {
        private static readonly Dictionary<Type, Type[]> Cache =
            new Dictionary<Type, Type[]>();

        public static List<Type> FindConcreteTypes<TBase>()
        {
            Type baseType =
                typeof(TBase);

            if (Cache.TryGetValue(
                baseType,
                out Type[] cached))
            {
                return new List<Type>(
                    cached);
            }

            TypeCache.TypeCollection types =
                TypeCache.GetTypesDerivedFrom(
                    baseType);

            List<Type> result =
                new List<Type>(
                    types.Count);

            for (int i = 0;
                 i < types.Count;
                 i++)
            {
                Type type =
                    types[i];

                if (type == null)
                {
                    continue;
                }

                if (type.IsAbstract ||
                    type.IsInterface)
                {
                    continue;
                }

                if (type.ContainsGenericParameters)
                {
                    continue;
                }

                if (!baseType.IsAssignableFrom(
                    type))
                {
                    continue;
                }

                result.Add(
                    type);
            }

            result =
                RemoveDuplicates(
                    result);

            Type[] finalResult =
                result.ToArray();

            Cache[baseType] =
                finalResult;

            return new List<Type>(
                finalResult);
        }

        private static List<Type> RemoveDuplicates(
            List<Type> types)
        {
            List<Type> result =
                new List<Type>(
                    types.Count);

            HashSet<Type> seen =
                new HashSet<Type>();

            for (int i = 0;
                 i < types.Count;
                 i++)
            {
                Type type =
                    types[i];

                if (type == null)
                {
                    continue;
                }

                if (!seen.Add(
                    type))
                {
                    continue;
                }

                result.Add(
                    type);
            }

            return result;
        }
    }
}