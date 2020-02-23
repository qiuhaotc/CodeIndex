using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeIndex.IndexBuilder
{
    public static class SimpleCodeContentProcessing
    {
        const string CharPrefix = "x7zmgdy7kcd";
        const string CharSuffix = "dktyc2bzsa";
        public const string HighLightPrefix = "0ffc7664bb0";
        public const string HighLightSuffix = "b17f5526cc3";

        public static string Preprocessing(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder();

            foreach (var character in content)
            {
                if (character >= 33 || character <= 126)
                {
                    if (SpecialCharRange.Any(u => u.Start <= character && u.End >= character))
                    {
                        stringBuilder.Append($" {CharPrefix}{character}{CharSuffix} ");
                    }
                    else
                    {
                        stringBuilder.Append(character);
                    }
                }
                else
                {
                    stringBuilder.Append(character);
                }
            }

            return stringBuilder.ToString();
        }

        static readonly Regex FindChar = new Regex($@"[ ]?{CharPrefix}(.{{1}}){CharSuffix}[ ]?");
        static readonly Regex FindCharWithPrefix = new Regex($@"[ ]?{HighLightPrefix}{CharPrefix}(.{{1}}){CharSuffix}{HighLightSuffix}[ ]?");

        public static string RestoreString(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                content = FindCharWithPrefix.Replace(content, u => $"{HighLightPrefix}{u.Groups[1].Value}{HighLightSuffix}");
                content = FindChar.Replace(content, u => u.Groups[1].Value);
            }

            return content;
        }

        readonly static HashSet<(int Start, int End)> SpecialCharRange = new HashSet<(int, int)>()
        {
            (33, 47),
            (58, 64),
            (91, 96),
            (123, 126)
        };
    }
}
