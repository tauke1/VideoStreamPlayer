using System.IO;
using System.Threading.Tasks;

namespace VideoStreamPlayer.StreamProviders
{
    public interface IStreamProvider
    {
        Task<Stream> GetStreamAsync(string urlOrPath);
    }
}
