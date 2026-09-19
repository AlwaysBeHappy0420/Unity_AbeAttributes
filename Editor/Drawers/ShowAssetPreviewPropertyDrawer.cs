#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public sealed class ShowAssetPreviewPropertyDrawer
        : AbeAttributeDrawer<ShowAssetPreviewAttribute>
    {
        protected override void DrawAttributePropertyLayout(
            AbeProperty property,
            GUIContent label,
            ShowAssetPreviewAttribute attribute)
        {
            SerializedProperty serializedProperty =
                property.SerializedProperty;

            if (serializedProperty == null)
            {
                AbeEditorGUI.HelpBox_Layout(
                    $"{property.Name} doesn't have a serialized property.",
                    MessageType.Warning);

                return;
            }

            if (serializedProperty.propertyType !=
                SerializedPropertyType.ObjectReference)
            {
                AbeEditorGUI.HelpBox_Layout(
                    $"{property.Name} doesn't have an asset preview.",
                    MessageType.Warning);

                return;
            }

            EditorGUILayout.PropertyField(
                serializedProperty,
                label);

            Object target =
                serializedProperty.objectReferenceValue;

            if (target == null)
            {
                return;
            }

            Texture2D previewTexture =
                AssetPreview.GetAssetPreview(target);

            if (previewTexture != null)
            {
                float width =
                    attribute.Width;

                float height =
                    attribute.Height;

                Rect rect =
                    EditorGUILayout.GetControlRect(
                        false,
                        height);

                rect.x +=
                    AbeEditorGUI.GetIndentLength(
                        rect);

                rect.width =
                    width;

                GUI.Label(
                    rect,
                    previewTexture);
            }
            else
            {
#if UNITY_6000_4_OR_NEWER
                bool loading =
                    AssetPreview.IsLoadingAssetPreview(
                        target.GetEntityId());
#else
                bool loading =
                    AssetPreview.IsLoadingAssetPreview(
                        target.GetInstanceID());
#endif

                if (loading)
                {
                    EditorWindow focused =
                        EditorWindow.focusedWindow;

                    if (focused != null)
                    {
                        focused.Repaint();
                    }
                }
            }
        }
    }
}

#endif