using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoStreamPlayer.HttpClients;
using VideoStreamPlayer.StreamProviders;

namespace VideoStreamPlayer
{
    public partial class MainForm : Form
    {
        private readonly IStreamClient _client;
        private readonly IStreamProvider _webStreamProvider;
        private readonly IStreamProvider _fileStreamProvider;
        private Task _fileReadBackgroundTask;

        public MainForm(IStreamClient client, WebStreamProvider webStreamProvider, FileStreamProvider fileStreamProvider)
        {
            _client = client;
            _webStreamProvider = webStreamProvider;
            _fileStreamProvider = fileStreamProvider;
            InitializeComponent();
            Task.Run(async () => await StartStreamLoading("http://83.128.74.78:8083/mjpg/video.mjpg", streetPictureBox, _webStreamProvider));
            Task.Run(async () => await StartStreamLoading("http://77.164.3.132/mjpg/video.mjpg", puppiesPictureBox, _webStreamProvider));
            Task.Run(async () => await StartStreamLoading("http://62.45.108.115:80/mjpg/video.mjpg", uretchtPictureBox, _webStreamProvider));
            _fileReadBackgroundTask = StartShowingStreamFromFile();
        }

        private Task StartShowingStreamFromFile()
        {
            return Task.Run(async () => await StartStreamLoading("Samples/sample_640x360.mjpeg", fromFilePictureBox, _fileStreamProvider));
        }

        private async Task StartStreamLoading(string pathOrUrl, PictureBox pictureBox, IStreamProvider streamProvider)
        {
            try
            {
                await _client.ReadStreamAsync(pathOrUrl, (s) => DrawPicture(s, pictureBox), streamProvider);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something wrong happened on loading stream - {pathOrUrl}\n{ex.Message}");
            }
        }

        private void DrawPicture(Stream stream, PictureBox pictureBox)
        {
            try
            {
                pictureBox.Image = Image.FromStream(stream);
            }
            catch (Exception ex)
            {
                // sometimes we can get corrupted data from mjpeg stream server, 
                // puppies stream sometimes returns bad byte sequence without start bytes pattern
                // so dont handle this case
            }
        }

        private void restartFileStreamReadingButton_Click(object sender, EventArgs e)
        {
            if (_fileReadBackgroundTask == null || _fileReadBackgroundTask.IsCompleted)
            {
                _fileReadBackgroundTask = StartShowingStreamFromFile();
            }
        }
    }
}
