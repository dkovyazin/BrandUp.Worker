using System.IO;
using System.Text;

namespace BrandUp.Worker.Remoting
{
    public interface IContractSerializer
    {
        string ContentType { get; }
        Encoding Encoding { get; }
        T Deserialize<T>(Stream stream, bool closeStream = true);
        void Serialize<T>(Stream stream, T data, bool closeStream = true);
    }
}