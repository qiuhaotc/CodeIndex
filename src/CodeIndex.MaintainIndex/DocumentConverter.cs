using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeIndex.Common;
using Lucene.Net.Documents;

namespace CodeIndex.MaintainIndex
{
    public static class DocumentConverter
    {
        static readonly ConcurrentDictionary<string, PropertyInfo[]> propertiesDictionary = new ConcurrentDictionary<string, PropertyInfo[]>();

        public static T GetObject<T>(this Document document) where T : new()
        {
            var type = typeof(T);
            var result = new T();

            if (!propertiesDictionary.TryGetValue(type.FullName ?? type.Name, out var propertyInfos))
            {
                propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite).ToArray();

                propertiesDictionary.TryAdd(nameof(T), propertyInfos);
            }

            foreach (var property in propertyInfos)
            {
                property.SetValue(result, GetValue(property, document));
            }

            return result;
        }

        static object GetValue(PropertyInfo property, Document document)
        {
            var propertyType = property.PropertyType;

            var value = GetValue(propertyType, document.Get(property.Name));

            if (value != null)
            {
                return value;
            }

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var genericType = propertyType.GetGenericArguments().First();
                var instance = Activator.CreateInstance(typeof(List<>).MakeGenericType(genericType));
                var collectionValues = document.Get(property.Name).Split(CodeIndexConfiguration.SplitChar).Where(u => !string.IsNullOrEmpty(u));
                var method = instance.GetType().GetMethod("Add");

                foreach (var sub in collectionValues)
                {
                    var subValue = GetValue(genericType, sub);

                    if (subValue == null)
                    {
                        throw new NotImplementedException($"Not able to set value for {property.Name}, type: {property.PropertyType}");
                    }

                    method?.Invoke(instance, new[] { subValue });
                }

                return instance;
            }

            throw new NotImplementedException($"Not able to set value for {property.Name}, type: {property.PropertyType}");
        }

        static object GetValue(Type type, string value)
        {
            if (type == typeof(string))
            {
                return value;
            }

            if (type == typeof(int))
            {
                return Convert.ToInt32(value);
            }

            if (type == typeof(DateTime))
            {
                return new DateTime(long.Parse(value));
            }

            if (type == typeof(Guid))
            {
                return new Guid(value);
            }

            if (type == typeof(double))
            {
                return Convert.ToDouble(value);
            }

            if (type == typeof(float))
            {
                return Convert.ToSingle(value);
            }

            return null;
        }
    }
}
