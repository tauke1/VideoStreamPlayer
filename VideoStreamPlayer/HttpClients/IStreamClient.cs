using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoStreamPlayer.StreamProviders;

namespace VideoStreamPlayer.HttpClients
{
    public interface IStreamClient
    {
        Task ReadStreamAsync(string url, Action<Stream> callback, IStreamProvider streamProvider);
    }
}
