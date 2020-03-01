using System;

namespace CodeIndex.Common
{
    public static class ExtendMethods
    {
        public static string SubStringSafe(this string str, int startIndex, int length)
        {
            var result = string.Empty;

            if (!string.IsNullOrEmpty(str))
            {
                length = Math.Min(length, str.Length - startIndex);

                if(length > 0)
                {
                    result = str.Substring(startIndex, length);
                }
            }

            return result;
        }
    }
}
