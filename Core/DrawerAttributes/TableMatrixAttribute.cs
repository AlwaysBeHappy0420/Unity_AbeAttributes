using System;

namespace AbeAttributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TableMatrixAttribute : DrawerAttribute
    {
        public string HorizontalTitle { get; set; }
        public string VerticalTitle { get; set; }

        public string DrawElementMethod { get; set; }

        public TableMatrixAttribute()
        {
        }
    }
}