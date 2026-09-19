using UnityEditor;

namespace AbeAttributes.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnityEngine.Object), true)]
    public class AbeInspector
        : UnityEditor.Editor
    {
        private AbePropertyTree _propertyTree;

        protected virtual void OnEnable()
        {
            _propertyTree =
                new AbePropertyTree(
                    serializedObject);
        }

        protected virtual void OnDisable()
        {
            _propertyTree?.Dispose();
        }

        public override void OnInspectorGUI()
        {
            _propertyTree.BeginDraw();

            _propertyTree.Draw();

            _propertyTree.EndDraw();
        }
    }
}
