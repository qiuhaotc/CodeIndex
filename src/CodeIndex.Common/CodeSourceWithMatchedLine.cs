namespace CodeIndex.Common
{
    public class CodeSourceWithMatchedLine
    {
        /// <summary>
        /// For Json Deserialize only
        /// </summary>
        public CodeSourceWithMatchedLine() {}

        public CodeSourceWithMatchedLine(CodeSource codeSource, int matchedLine, string matchedContent)
        {
            CodeSource = codeSource;
            MatchedLine = matchedLine;
            MatchedContent = matchedContent;
        }

        public CodeSource CodeSource { get; set; }
        public int MatchedLine { get; set; }
        public string MatchedContent { get; set; }
    }
}
