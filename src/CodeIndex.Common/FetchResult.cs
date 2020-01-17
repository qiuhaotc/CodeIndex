namespace CodeIndex.Common
{
    public class FetchResult<T>
    {
        public Status Status { get; set; }
        public T Result { get; set; }
    }
}
