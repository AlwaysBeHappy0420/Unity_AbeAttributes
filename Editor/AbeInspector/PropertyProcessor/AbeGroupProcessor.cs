using System;
using System.Collections.Generic;
using System.Linq;

namespace AbeAttributes.Editor
{
    public sealed class AbeGroupProcessor
        : AbePropertyProcessor
    {
        private sealed class GroupContext
        {
            public AbeProperty Group;
        }

        public override void Process(
            AbePropertyTree tree,
            List<AbeProperty> properties)
        {
            if (properties == null ||
                properties.Count == 0)
            {
                return;
            }

            List<AbeProperty> result =
                new List<AbeProperty>(
                    properties.Count);

            Stack<GroupContext> groupStack =
                new Stack<GroupContext>();

            for (int i = 0;
                 i < properties.Count;
                 i++)
            {
                AbeProperty property =
                    properties[i];

                if (property == null)
                {
                    continue;
                }

                GroupStartAttribute[] starts =
                    property.Attributes
                        .OfType<GroupStartAttribute>()
                        .ToArray();

                int endCount =
                    property.Attributes
                        .OfType<GroupEndAttribute>()
                        .Count();

                // ========================================================
                // Start Groups
                //
                // GroupStart is processed BEFORE the current property is
                // added, therefore the current property becomes the first
                // child of the new group.
                // ========================================================

                for (int startIndex = 0;
                     startIndex < starts.Length;
                     startIndex++)
                {
                    GroupStartAttribute start =
                        starts[startIndex];

                    string structureKey =
                        BuildGroupStructureKey(
                            property,
                            start,
                            startIndex);

                    AbeProperty group =
                        AbeProperty.CreateGroup(
                            tree,
                            start,
                            structureKey,
                            property.TargetObject);

                    // Nested group
                    if (groupStack.Count > 0)
                    {
                        groupStack
                            .Peek()
                            .Group
                            .AddChild(group);
                    }
                    else
                    {
                        result.Add(group);
                    }

                    groupStack.Push(
                        new GroupContext
                        {
                            Group = group
                        });
                }

                // ========================================================
                // Current Property
                //
                // If a group is active, this property belongs to the
                // deepest active group.
                // ========================================================

                if (groupStack.Count > 0)
                {
                    groupStack
                        .Peek()
                        .Group
                        .AddChild(property);
                }
                else
                {
                    result.Add(property);
                }

                // ========================================================
                // End Groups
                //
                // GroupEnd belongs to the current property, so the current
                // property has already been added before ending the group.
                // ========================================================

                for (int endIndex = 0;
                     endIndex < endCount;
                     endIndex++)
                {
                    if (groupStack.Count == 0)
                    {
                        // No group to close.
                        // Ignore invalid GroupEnd safely.
                        continue;
                    }

                    groupStack.Pop();
                }
            }

            // ============================================================
            // Replace original property list
            // ============================================================

            properties.Clear();

            properties.AddRange(result);
        }

        private static string BuildGroupStructureKey(
            AbeProperty property,
            GroupStartAttribute attribute,
            int index)
        {
            return "group:"
                   + property.StructureKey
                   + ":"
                   + attribute.Name
                   + ":"
                   + index;
        }
    }
}