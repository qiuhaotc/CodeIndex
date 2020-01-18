using System.Collections.Generic;

namespace CodeIndex.Common
{
    public class Storage
    {
        Dictionary<string, object> Items { get; set; } = new Dictionary<string, object>();
        public string UserName { get; set; }

        public object GetValue(string key)
        {
            return Items.ContainsKey(key) ? Items[key] : null;
        }

        public void SetOrUpdate(string key, object value)
        {
            Items[key] = value;
        }
    }
}
