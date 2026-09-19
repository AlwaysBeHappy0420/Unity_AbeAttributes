namespace AbeAttributes.Editor
{
    public sealed class AbePropertyContext
    {
        private readonly AbePropertyTree _tree;

        private readonly string _propertyKey;

        internal AbePropertyContext(
            AbePropertyTree tree,
            string propertyKey)
        {
            _tree =
                tree;

            _propertyKey =
                propertyKey
                ?? string.Empty;
        }

        internal SavedBool GetSavedBool(
            string key,
            bool defaultValue)
        {
            if (_tree == null)
            {
                return new SavedBool(
                    "Abe:invalid:"
                    + _propertyKey
                    + ":"
                    + key,
                    defaultValue);
            }

            return _tree.GetSavedBool(
                _propertyKey,
                key,
                defaultValue);
        }

        public void Clear()
        {
            if (_tree == null)
            {
                return;
            }

            _tree.ClearSavedBools(
                _propertyKey);
        }
    }
}