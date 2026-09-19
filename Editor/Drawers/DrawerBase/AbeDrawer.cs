using System;
using UnityEngine;

namespace AbeAttributes.Editor
{
    public abstract class AbeDrawer
    {
        public AbeDrawer Next { get; internal set; }

        public void Draw(
            AbeProperty property,
            GUIContent label)
        {
            DrawPropertyLayout(
                property,
                label);
        }

        protected abstract void DrawPropertyLayout(
            AbeProperty property,
            GUIContent label);

        protected void CallNextDrawer(
            AbeProperty property,
            GUIContent label)
        {
            if (Next != null)
            {
                Next.Draw(
                    property,
                    label);
            }
        }
    }

    [AttributeUsage(
        AttributeTargets.Class,
        Inherited = false)]
    public sealed class AbeDrawerPriorityAttribute
        : Attribute
    {
        public int Value { get; }

        public AbeDrawerPriorityAttribute(
            int value)
        {
            Value = value;
        }
    }

    [AttributeUsage(
        AttributeTargets.Class,
        Inherited = false)]
    internal sealed class AbePropertyKindDrawerAttribute
        : Attribute
    {
        public AbePropertyKind Kind { get; }

        public AbePropertyKindDrawerAttribute(
            AbePropertyKind kind)
        {
            Kind = kind;
        }
    }

    public abstract class AbeValueDrawer<TValue>
        : AbeDrawer
    {
        protected sealed override void DrawPropertyLayout(
            AbeProperty property,
            GUIContent label)
        {
            DrawValuePropertyLayout(
                property,
                label);
        }

        protected abstract void DrawValuePropertyLayout(
            AbeProperty property,
            GUIContent label);
    }

    public abstract class AbeAttributeDrawer<TAttribute>
        : AbeDrawer
        where TAttribute : Attribute
    {
        protected sealed override void DrawPropertyLayout(
            AbeProperty property,
            GUIContent label)
        {
            TAttribute attribute =
                property.GetAttribute<TAttribute>();

            if (attribute == null)
            {
                CallNextDrawer(
                    property,
                    label);

                return;
            }

            DrawAttributePropertyLayout(
                property,
                label,
                attribute);
        }

        protected abstract void DrawAttributePropertyLayout(
            AbeProperty property,
            GUIContent label,
            TAttribute attribute);
    }

    public abstract class AbeGroupDrawer<TAttribute>
        : AbeDrawer
        where TAttribute : Attribute
    {
        protected sealed override void DrawPropertyLayout(
            AbeProperty property,
            GUIContent label)
        {
            TAttribute attribute =
                property.GetAttribute<TAttribute>();

            if (attribute == null)
            {
                CallNextDrawer(
                    property,
                    label);

                return;
            }

            DrawGroupPropertyLayout(
                property,
                label,
                attribute);
        }

        protected abstract void DrawGroupPropertyLayout(
            AbeProperty property,
            GUIContent label,
            TAttribute attribute);

        protected static void DrawChildren(
            AbeProperty group)
        {
            foreach (AbeProperty child
                in group.Children)
            {
                child.Draw();
            }
        }
    }

    // ====================================================================
    // Property Kind Drawer
    //
    // Used by framework-owned property-kind drawers such as:
    //
    // Serialized
    // Method
    // Group fallback
    // ====================================================================

    internal abstract class AbePropertyKindDrawer
        : AbeDrawer
    {
        internal abstract AbePropertyKind PropertyKind { get; }

        protected sealed override void DrawPropertyLayout(
            AbeProperty property,
            GUIContent label)
        {
            if (property == null ||
                property.Kind != PropertyKind)
            {
                CallNextDrawer(
                    property,
                    label);

                return;
            }

            DrawPropertyKindLayout(
                property,
                label);
        }

        protected abstract void DrawPropertyKindLayout(
            AbeProperty property,
            GUIContent label);
    }
}