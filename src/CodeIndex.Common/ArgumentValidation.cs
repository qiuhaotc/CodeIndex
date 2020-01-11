using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeIndex.Common
{
    public static class ArgumentValidation
    {
        public static void RequireContainsElement<T>(this IEnumerable<T> value, string argumentName)
        {
            if (value == null || !value.Any())
            {
                throw new ArgumentException("The collection can't be null or empty", argumentName);
            }
        }

        public static void RequireNotNullOrEmpty(this string value, string argumentName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("The value can't be null or empty", argumentName);
            }
        }

        public static void RequireNotNull(this object value, string argumentName)
        {
            if (value == null)
            {
                throw new ArgumentException("The value can't be null", argumentName);
            }
        }

        public static void RequireRange(this int value, string argumentName, int maxValue, int minValue)
        {
            if (value> maxValue || value < minValue)
            {
                throw new ArgumentException($"The value should in range ({minValue} - {maxValue})", argumentName);
            }
        }
    }
}
