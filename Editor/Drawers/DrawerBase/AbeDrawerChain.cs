using UnityEngine;

namespace AbeAttributes.Editor
{
    internal static class AbeDrawerChain
    {
        public static void Draw(
            AbeProperty property,
            GUIContent label,
            ref AbeDrawer chain)
        {
            if (chain == null)
            {
                chain =
                    property.Tree
                        .DrawerLocator
                        .CreateChain(property);
            }

            chain.Draw(
                property,
                label);
        }
    }
}
