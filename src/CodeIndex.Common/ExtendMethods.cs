using System;
using System.Collections.Generic;
using System.Text;

namespace CodeIndex.Common
{
    public static class ExtendMethods
    {
        public static string SubStringSafe(this string str, int startIndex, int length)
        {
            var result = string.Empty;

            if (!string.IsNullOrEmpty(str))
            {
                result = str.Substring(startIndex, Math.Min(length, str.Length - startIndex));
            }

            return result;
        } 
    }
}
