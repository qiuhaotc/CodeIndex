using System;

namespace CodeIndex.Common
{
    public static class ExtendMethods
    {
        public static string SubStringSafeWithEllipsis(this string str, int startIndex, int length, string ellipsis = "...")
        {
            var result = str.SubStringSafe(startIndex, length);
            return result.Length == str.Length ? result : (startIndex <= 0 ? result + ellipsis : ellipsis + result);
        }

        public static string SubStringSafe(this string str, int startIndex, int length)
        {
            var result = string.Empty;
            startIndex = startIndex >= 0 ? startIndex : 0;

            if (!string.IsNullOrEmpty(str))
            {
                length = Math.Min(length, str.Length - startIndex);

                if (length > 0)
                {
                    result = str.Substring(startIndex, length);
                }
            }

            return result;
        }
    }
}
