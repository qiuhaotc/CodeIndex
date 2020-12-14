using System.Collections.Generic;
using System.Linq;
using CodeIndex.Common;

namespace CodeIndex.IndexBuilder
{
    /// <summary>
    /// Reference the SmartCn WordSegmenter
    /// </summary>
    public class WordSegmenter
    {
        /// <summary>
        /// Segment a sentence into words with <see cref="WordSegmenter"/>
        /// </summary>
        /// <param name="sentence">input sentence</param>
        /// <param name="startOffset"> start offset of sentence</param>
        /// <returns><see cref="IList{T}"/> of <see cref="SimpleSegToken"/>.</returns>
        public virtual IList<SimpleSegToken> SegmentSentence(string sentence, int startOffset)
        {
            var segTokenList = GetSegToken(sentence);

            foreach (SimpleSegToken st in segTokenList)
            {
                ConvertSegToken(st, sentence, startOffset);
            }

            return segTokenList;
        }

        List<SimpleSegToken> emptySegTokenList = new List<SimpleSegToken>();

        IList<SimpleSegToken> GetSegToken(string sentence)
        {
            var segTokenList = emptySegTokenList;

            if (!string.IsNullOrEmpty(sentence))
            {
                var charArray = sentence.ToCharArray();

                segTokenList = new List<SimpleSegToken>();
                var length = 0;
                var startIndex = -1;

                for (var index = 0; index < charArray.Length; index++)
                {
                    if (!SpaceLike(charArray[index]))
                    {
                        var charInt = (int)charArray[index];
                        if (IsSpecialChar(charInt))
                        {
                            AddSegTokenIfNeeded();

                            segTokenList.Add(new SimpleSegToken(charArray, index, index + 1));
                        }
                        else
                        {
                            if (startIndex == -1)
                            {
                                startIndex = index;
                            }

                            length++;
                        }
                    }
                    else
                    {
                        AddSegTokenIfNeeded();
                    }
                }

                AddSegTokenIfNeeded();

                void AddSegTokenIfNeeded()
                {
                    if (length > 0)
                    {
                        segTokenList.Add(new SimpleSegToken(charArray, startIndex, startIndex + length));
                        length = 0;
                        startIndex = -1;
                    }
                }
            }

            return segTokenList;
        }

        public virtual SimpleSegToken ConvertSegToken(SimpleSegToken st, string sentence,
            int sentenceStartOffset)
        {
            st.CharArray = sentence.Substring(st.StartOffset, st.EndOffset - st.StartOffset).ToCharArray();
            st.StartOffset += sentenceStartOffset;
            st.EndOffset += sentenceStartOffset;

            return st;
        }

        static bool SpaceLike(char ch)
        {
            return ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n' || ch == '　';
        }

        static bool IsSpecialChar(int character) => (character >= 33 && character <= 126) && SpecialCharRange.Any(u => u.Start <= character && u.End >= character);

        static readonly HashSet<(int Start, int End)> SpecialCharRange = new HashSet<(int, int)>()
        {
            (33, 47),
            (58, 64),
            (91, 96),
            (123, 126)
        };

        public static string[] GetWords(string content)
        {
            content.RequireNotNull(nameof(content));

            var words = new List<string>();
            var chars = new List<char>();

            foreach (var ch in content)
            {
                if (!IsSpecialChar(ch) && !SpaceLike(ch))
                {
                    chars.Add(ch);
                }
                else if (chars.Count > 0)
                {
                    words.Add(new string(chars.ToArray()));
                    chars.Clear();
                }
            }

            if (chars.Count > 0)
            {
                words.Add(new string(chars.ToArray()));
            }

            return words.ToArray();
        }
    }
}