using System.IO;
using System.Text.Json;

namespace eShopLegacy.Utilities
{
    public class Serializing
    {
        public Stream SerializeBinary(object input)
        {
            var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, input, input.GetType());
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public object DeserializeBinary(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, leaveOpen: true);
            var json = reader.ReadToEnd();
            stream.Seek(0, SeekOrigin.Begin);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
    }
}
