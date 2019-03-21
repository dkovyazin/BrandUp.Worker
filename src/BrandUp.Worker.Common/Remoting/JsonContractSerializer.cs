using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace BrandUp.Worker.Remoting
{
    public class JsonContractSerializer : IContractSerializer
    {
        private readonly JsonSerializerSettings serializerSettings;
        private readonly JsonSerializer jsonSerializer;

        public JsonContractSerializer()
        {
            serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Auto
            };
            jsonSerializer = JsonSerializer.Create(serializerSettings);

            Encoding = new System.Text.UTF8Encoding(false);
        }

        public string ContentType => "application/json";
        public Encoding Encoding { get; private set; }
        public T Deserialize<T>(Stream stream, bool closeStream = true)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var streamReader = new StreamReader(stream, Encoding, false, 1024, !closeStream))
            {
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    jsonReader.CloseInput = closeStream;

                    return jsonSerializer.Deserialize<T>(jsonReader);
                }
            }
        }
        public void Serialize<T>(Stream stream, T data, bool closeStream = true)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var streamWriter = new StreamWriter(stream, Encoding, 1024, !closeStream))
            {
                using (var jsonWriter = new JsonTextWriter(streamWriter))
                {
                    jsonWriter.CloseOutput = closeStream;

                    jsonSerializer.Serialize(jsonWriter, data);
                }
            }
        }
    }
}