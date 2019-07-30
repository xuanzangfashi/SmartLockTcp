using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Json
{
    public class JsonObject
    {
        public JsonObject()
        {
            jobj = new List<JObject>();
        }
        public List<JObject> jobj;
        public string jstr;
    }

    public static class JsonWorker
    {
        public static JsonObject MakeSampleJson(string[] keys, string[] values)
        {
            if (keys.Length != values.Length)
                return null;
            JObject jobj = new JObject();
            for (int i = 0; i < keys.Length; i++)
            {
                jobj[keys[i]] = values[i];
            }
            var jsonStr = jobj.ToString();
            jsonStr = jsonStr.Replace(" ", "");
            JsonObject tmpJsonObj = new JsonObject();
            tmpJsonObj.jobj.Add(jobj);
            tmpJsonObj.jstr = jsonStr;
            return tmpJsonObj;
        }

        public static JsonObject MakeSampleReturnJson(string[] additionalKeys,string[] values)
        {
            string[] defultKeys = { "type", "result", "code" };
            List<string> keys = new List<string>();
            keys.AddRange(defultKeys);
            if (additionalKeys != null)
            {
                keys.AddRange(additionalKeys);
            }
            return MakeSampleJson(keys.ToArray(), values);
        }
    }
}
