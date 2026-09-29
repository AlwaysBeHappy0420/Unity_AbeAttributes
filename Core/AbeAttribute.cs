using System;

namespace AbeAttributes
{
    public abstract class AbeAttribute : Attribute
    {
        public virtual bool ApplyToCollectionElement => false;
    }
}
