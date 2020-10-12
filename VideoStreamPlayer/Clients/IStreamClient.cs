using System;
using System.IO;
using System.Threading.Tasks;
using VideoStreamPlayer.StreamProviders;

namespace VideoStreamPlayer.Clients
{
    public interface IStreamClient
    {
        Task ReadStreamAsync(string url, Action<Stream> callback, IStreamProvider streamProvider);
    }
}
