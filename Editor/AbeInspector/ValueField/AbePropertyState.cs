namespace AbeAttributes.Editor
{
    public sealed class AbePropertyState
    {
        public bool Visible { get; private set; } = true;

        public bool Enabled { get; private set; } = true;

        public bool HasMultipleDifferentValues { get; internal set; }

        public void Reset()
        {
            Visible = true;
            Enabled = true;
            HasMultipleDifferentValues = false;
        }

        public void SetVisible(bool value)
        {
            Visible &= value;
        }

        public void SetEnabled(bool value)
        {
            Enabled &= value;
        }
    }
}