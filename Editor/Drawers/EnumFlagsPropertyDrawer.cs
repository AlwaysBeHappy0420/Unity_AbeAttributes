using System;
using UnityEditor;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public sealed class EnumFlagsPropertyDrawer
        : AbeAttributeDrawer<EnumFlagsAttribute>
    {
        protected override void DrawAttributePropertyLayout(
            AbeProperty property,
            GUIContent label,
            EnumFlagsAttribute attribute)
        {
            Enum targetEnum =
                property.ValueEntry.GetValue() as Enum;

            if (targetEnum == null)
            {
                AbeEditorGUI.HelpBox_Layout(
                    attribute.GetType().Name
                    + " can be used only on enums",
                    MessageType.Warning,
                    context: property.Tree.Target);

                return;
            }

            Enum enumNew =
                EditorGUILayout.EnumFlagsField(
                    label,
                    targetEnum);

            if (!Equals(
                    enumNew,
                    targetEnum))
            {
                property.ValueEntry.SetValue(
                    enumNew);
            }
        }
    }
}