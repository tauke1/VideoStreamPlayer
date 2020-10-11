using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
