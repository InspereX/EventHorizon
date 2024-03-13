namespace Insperex.EventHorizon.Abstractions.Compression
{
    public interface ISerializer
    {
        public string Serialize(object obj);
        public object Deserialize(string str);

        public string Serialize<T>(T obj);
        public T Deserialize<T>(string str);
    }
}
