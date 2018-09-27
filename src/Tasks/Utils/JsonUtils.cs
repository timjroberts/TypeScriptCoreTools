using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tasks.Utils
{
    public static class JsonUtils
    {
        public static JObject LoadJObject(FileInfo jsonFile)
        {
            using (var reader = File.OpenText(jsonFile.FullName))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return (JObject)JToken.ReadFrom(jsonReader);
            }
        }

        public static void SaveJObject(JObject obj, FileInfo jsonFile)
        {
            using (var writer = File.CreateText(jsonFile.FullName))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                jsonWriter.Formatting = Formatting.Indented;
                
                obj.WriteTo(jsonWriter);
            }
        }
    }
}