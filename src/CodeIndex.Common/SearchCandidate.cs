namespace CodeIndex.Common
{
    public class SearchCandidate
    {
        public SearchType SearchType { get; set; }
        public string SearchText { get; set; }
        public bool IsAndCondition { get; set; }
    }
}
