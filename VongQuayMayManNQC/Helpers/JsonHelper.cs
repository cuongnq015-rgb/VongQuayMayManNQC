using Newtonsoft.Json;
using System.IO;

namespace VongQuayMayManNQC.Helpers
{
    public static class JsonHelper
    {
        public static void Save<T>(string filePath, T data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public static T Load<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return default(T);

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}