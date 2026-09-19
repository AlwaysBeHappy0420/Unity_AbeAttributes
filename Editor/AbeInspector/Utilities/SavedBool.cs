#if UNITY_EDITOR

using UnityEditor;

namespace AbeAttributes.Editor
{
    internal sealed class SavedBool
    {
        private readonly string _name;
        private bool _value;

        public bool value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value == value)
                {
                    return;
                }

                _value = value;

                EditorPrefs.SetBool(
                    _name,
                    value);
            }
        }

        public SavedBool(
            string name,
            bool defaultValue)
        {
            _name =
                name;

            _value =
                EditorPrefs.GetBool(
                    name,
                    defaultValue);
        }
    }
}

#endif