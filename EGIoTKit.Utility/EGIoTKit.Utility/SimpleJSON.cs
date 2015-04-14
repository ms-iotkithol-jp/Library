using System;
using Microsoft.SPOT;
using System.Text;

namespace EGIoTKit.Utility
{
    /// <summary>
    /// Very very simple JSON serializer
    /// Example:
    /// var sJson = new SimpleJSON();
    /// sJson.Add("key1","value");
    /// sJson.Add("key2",numeric);
    /// ...
    /// var serialized = sJson.GetResult();
    /// </summary>
    public class SimpleJSON
    {
        StringBuilder sb;
        public SimpleJSON()
        {
            sb = new StringBuilder("");
        }
        public void Add(string key, string value)
        {
            AddPre();
            AddKey(key);
            sb.Append("\"" + value + "\"");
        }
        public void Add(string key, double value)
        {
            AddPre();
            AddKey(key);
            sb.Append(value);
        }

        public string GetResult()
        {
            return sb.Append("}").ToString();
        }

        void AddPre()
        {
            if (sb.Length == 0)
            {
                sb.Append("{");
            }
            else
            {
                sb.Append(",");
            }
        }
        void AddKey(string key)
        {
            sb.Append("\"" + key + "\":\"");
        }
    }
}
