using AbeAttributes;

namespace AbeAttributes.Editor
{
    public sealed class AbeReadOnlyStateUpdater
        : AbeStateUpdater
    {
        public override void Update(
            AbeProperty property)
        {
            if (!property.HasAttribute<ReadOnlyAttribute>())
            {
                return;
            }

            property.State.SetEnabled(false);
        }
    }
}