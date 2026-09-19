using System;
using System.Collections.Generic;
using System.Reflection;

namespace AbeAttributes.Editor
{
    internal static class AbeAttributeUtility
    {
        private static readonly Dictionary<
            MemberInfo,
            Attribute[]>
            Cache =
                new Dictionary<
                    MemberInfo,
                    Attribute[]>();

        // ================================================================
        // Has
        // ================================================================

        public static bool HasAbeAttribute(
            MemberInfo member)
        {
            if (member == null)
            {
                return false;
            }

            Attribute[] attributes =
                GetCachedAbeAttributes(
                    member);

            return attributes.Length > 0;
        }

        // ================================================================
        // Get
        // ================================================================

        public static List<Attribute>
            GetAbeAttributes(
                MemberInfo member)
        {
            List<Attribute> result =
                new List<Attribute>();

            if (member == null)
            {
                return result;
            }

            Attribute[] attributes =
                GetCachedAbeAttributes(
                    member);

            for (int i = 0;
                 i < attributes.Length;
                 i++)
            {
                result.Add(
                    attributes[i]);
            }

            return result;
        }

        // ================================================================
        // Cache
        // ================================================================

        private static Attribute[]
            GetCachedAbeAttributes(
                MemberInfo member)
        {
            if (Cache.TryGetValue(
                member,
                out Attribute[] cached))
            {
                return cached;
            }

            object[] attributes =
                member.GetCustomAttributes(
                    true);

            List<Attribute> AbeAttributes =
                new List<Attribute>();

            for (int i = 0;
                 i < attributes.Length;
                 i++)
            {
                object attribute =
                    attributes[i];

                if (!(attribute
                    is AbeAttributes.IAbeAttribute))
                {
                    continue;
                }

                if (!(attribute
                    is Attribute AbeAttribute))
                {
                    continue;
                }

                AbeAttributes.Add(
                    AbeAttribute);
            }

            Attribute[] result =
                AbeAttributes.ToArray();

            Cache[member] =
                result;

            return result;
        }
    }
}