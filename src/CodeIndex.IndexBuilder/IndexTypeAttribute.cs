using System;

namespace CodeIndex.IndexBuilder
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IndexTypeAttribute : Attribute
    {
        public IndexTypeAttribute(IndexTypes indexTypes)
        {
            IndexType = indexTypes;
        }

        public IndexTypes IndexType { get; }
    }

    public enum IndexTypes
    {
        Default,
        StringType,
        TextType,
        StoredType,
        Number
    }
}
