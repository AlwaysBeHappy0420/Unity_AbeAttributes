using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    [AbePropertyKindDrawer(
        AbePropertyKind.Serialized)]
    internal sealed class AbeSerializedPropertyDrawer
        : AbePropertyKindDrawer
    {
        internal override AbePropertyKind PropertyKind =>
            AbePropertyKind.Serialized;

        protected override void DrawPropertyKindLayout(
            AbeProperty property,
            GUIContent label)
        {
            SerializedProperty serializedProperty =
                property.SerializedProperty;

            if (serializedProperty == null)
            {
                return;
            }

            if (serializedProperty.propertyPath ==
                "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(
                        serializedProperty,
                        label,
                        false);
                }

                return;
            }

            if (AbeCollectionUtility
                .IsSerializedCollection(
                    serializedProperty))
            {
                CallNextDrawer(
                    property,
                    label);

                return;
            }

            using (new EditorGUI.DisabledScope(
                !property.State.Enabled))
            {
                EditorGUILayout.PropertyField(
                    serializedProperty,
                    label,
                    false);
            }

            if (!serializedProperty.hasVisibleChildren)
            {
                return;
            }

            if (!serializedProperty.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            IReadOnlyList<AbeProperty> children =
                property.Children;

            for (int i = 0;
                 i < children.Count;
                 i++)
            {
                children[i].Draw();
            }

            EditorGUI.indentLevel--;
        }
    }
}