using Newtonsoft.Json;

namespace Insperex.EventHorizon.Abstractions.Serialization.Json
{
    public class NewtonsoftJsonSerializer : ISerializer
    {
        public string Serialize(object obj) => JsonConvert.SerializeObject(obj);

        public object Deserialize(string str) => JsonConvert.DeserializeObject(str);

        public string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj);

        public T Deserialize<T>(string str) => JsonConvert.DeserializeObject<T>(str);
    }
}
