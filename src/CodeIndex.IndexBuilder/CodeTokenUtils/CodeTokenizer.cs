using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ICU4N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;

namespace CodeIndex.IndexBuilder
{
    /// <summary>
    /// Reference the SmartCn Tokenizer
    /// </summary>
    internal sealed class CodeTokenizer : SegmentingTokenizerBase
    {
        static readonly BreakIterator sentenceProto = BreakIterator.GetSentenceInstance(CultureInfo.InvariantCulture);
        readonly WordSegmenter wordSegmenter = new WordSegmenter();

        readonly ICharTermAttribute termAtt;
        readonly IOffsetAttribute offsetAtt;

        IEnumerator<SimpleSegToken> tokens;

        public CodeTokenizer(TextReader reader) : base(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, reader, (BreakIterator)sentenceProto.Clone())
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        protected override void SetNextSentence(int sentenceStart, int sentenceEnd)
        {
            var sentence = new string(m_buffer, sentenceStart, sentenceEnd - sentenceStart);
            tokens = wordSegmenter.SegmentSentence(sentence, m_offset + sentenceStart).GetEnumerator();
        }

        protected override bool IncrementWord()
        {
            if (tokens == null || !tokens.MoveNext())
            {
                return false;
            }
            else
            {
                var token = tokens.Current;
                ClearAttributes();
                termAtt.CopyBuffer(token.CharArray, 0, token.CharArray.Length);
                offsetAtt.SetOffset(CorrectOffset(token.StartOffset), CorrectOffset(token.EndOffset));
                return true;
            }
        }

        public override void Reset()
        {
            base.Reset();
            tokens = null;
        }
    }
}
