using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VideoStreamPlayer.StreamProviders
{
    public class WebStreamProvider : IStreamProvider
    {
        private readonly HttpClient _httpClient;
        public WebStreamProvider()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public Task<Stream> GetStreamAsync(string url)
        {
            return _httpClient.GetStreamAsync(url);
        }
    }
}
