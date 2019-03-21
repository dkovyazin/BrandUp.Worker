using System.Net.Http;
using System.Threading.Tasks;

namespace BrandUp.Worker.Remoting
{
    public static class IContractSerializerExtensions
    {
        public static JsonContent<T> CreateJsonContent<T>(this IContractSerializer contractSerializer, T data)
            where T : class
        {
            return new JsonContent<T>(data, contractSerializer);
        }

        public static async Task<T> DeserializeHttpResponseAsync<T>(this IContractSerializer contractSerializer, HttpResponseMessage response)
        {
            return contractSerializer.Deserialize<T>(await response.Content.ReadAsStreamAsync(), false);
        }
    }
}