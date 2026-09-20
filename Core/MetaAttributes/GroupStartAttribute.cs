using System;

namespace AbeAttributes
{
    public enum GroupStyle
    {
        Box,
        Vertical,
        Horizontal
    }

    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = true)]
    public sealed class GroupStartAttribute
        :GroupAttribute
    {

        public string Name { get; }

        public string ShowIf { get; }

        public GroupStyle Style { get; }

        // [GroupStart]
        public GroupStartAttribute()
            : this(
                "Group1",
                null,
                GroupStyle.Box)
        {
        }

        // [GroupStart("Advanced")]
        public GroupStartAttribute(
            string name)
            : this(
                name,
                null,
                GroupStyle.Box)
        {
        }

        // [GroupStart("Advanced", "IsAdvanced")]
        public GroupStartAttribute(
            string name,
            string showIf)
            : this(
                name,
                showIf,
                GroupStyle.Box)
        {
        }

        // [GroupStart("Advanced", GroupStyle.Horizontal)]
        // public GroupStartAttribute(
        //     string name,
        //     GroupStyle style)
        //     : this(
        //         name,
        //         null,
        //         style)
        // {
        // }

        // [GroupStart(
        //     "Advanced",
        //     "IsAdvanced",
        //     GroupStyle.Box)]
        public GroupStartAttribute(
            string name,
            string showIf,
            GroupStyle style)
        {
            Name =
                string.IsNullOrEmpty(name)
                    ? "Group1"
                    : name;

            ShowIf =
                showIf;

            Style = GroupStyle.Box;
        }
    }
}