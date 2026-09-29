using System;

namespace AbeAttributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class EnumFlagsAttribute : DrawerAttribute
    {
        public override bool ApplyToCollectionElement => true;
    }
}
