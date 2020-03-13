namespace CodeIndex.IndexBuilder
{
    public class SimpleSegToken
    {
        public SimpleSegToken(char[] charArray, int startOffset, int endOffset)
        {
            CharArray = charArray;
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public char[] CharArray { get; set; }
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
    }
}
