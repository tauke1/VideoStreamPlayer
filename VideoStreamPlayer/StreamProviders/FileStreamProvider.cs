using System.IO;
using System.Threading.Tasks;

namespace VideoStreamPlayer.StreamProviders
{
    public class FileStreamProvider : IStreamProvider
    {
        public Task<Stream> GetStreamAsync(string path)
        {
            return Task.FromResult((Stream)File.OpenRead(path));
        }
    }
}
