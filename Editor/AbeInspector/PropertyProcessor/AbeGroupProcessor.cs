using System.Collections.Generic;
using System.Linq;

namespace AbeAttributes.Editor
{
    public sealed class AbeGroupProcessor
        : AbePropertyProcessor
    {
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
                new List<AbeProperty>();

            Dictionary<string, AbeProperty> groups =
                new Dictionary<string, AbeProperty>();

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

                IGroupAttribute groupAttribute =
                    property.Attributes
                        .OfType<IGroupAttribute>()
                        .FirstOrDefault();

                // ========================================================
                // Normal property
                // ========================================================

                if (groupAttribute == null)
                {
                    result.Add(
                        property);

                    continue;
                }

                // ========================================================
                // Group key
                // ========================================================

                string key =
                    groupAttribute.GetType().FullName
                    + ":"
                    + groupAttribute.Name;

                // ========================================================
                // Create group
                // ========================================================

                if (!groups.TryGetValue(
                        key,
                        out AbeProperty group))
                {
                    group =
                        AbeProperty.CreateGroup(
                            tree,
                            groupAttribute);

                    groups.Add(
                        key,
                        group);

                    result.Add(
                        group);
                }

                // ========================================================
                // Add property
                // ========================================================

                group.AddChild(
                    property);
            }

            // ============================================================
            // Replace
            // ============================================================

            properties.Clear();

            properties.AddRange(
                result);
        }
    }
}