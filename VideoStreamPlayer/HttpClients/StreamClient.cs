using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoStreamPlayer.Models;
using VideoStreamPlayer.StreamProviders;

namespace VideoStreamPlayer.HttpClients
{
    public class StreamClient : IStreamClient
    {
        private static readonly byte[] JpegEndPattern = new byte[] { 0xFF, 0xD9 };
        private static readonly byte[] JpegStartPattern = new byte[] { 0xFF, 0xD8 };
        private const int BUFFER_SIZE = 1024;
        public StreamClient()
        {
        }

        /// <summary>
        ///  this method is public because, it need to be tested
        /// </summary>
        /// <param name="currentBuffer"></param>
        /// <param name="currentBufferBytesRead"></param>
        /// <param name="bufferPrevState"></param>
        /// <param name="bufferPrevStateReadBytes"></param>
        /// <returns></returns>
        public KeyValuePair<List<MJpegFrameRange>, byte[]> GetJpegStartAndEndPositions(byte[] currentBuffer, int currentBufferBytesRead, byte[] bufferPrevState, int bufferPrevStateReadBytes)
        {
            if (currentBuffer == null)
                throw new ArgumentNullException(nameof(currentBuffer));

            if (currentBufferBytesRead > currentBuffer.Length)
                throw new InvalidOperationException($"{nameof(currentBufferBytesRead)} should be equal or less than {nameof(currentBuffer)} argument's length");

            if (bufferPrevState != null && bufferPrevState.Length != currentBuffer.Length)
                throw new InvalidOperationException($"{nameof(bufferPrevState)} argument's length should be equal to {nameof(currentBuffer)} argument's length");

            if (bufferPrevState != null && bufferPrevStateReadBytes > bufferPrevState.Length)
                throw new InvalidOperationException($"{nameof(bufferPrevStateReadBytes)} should be equal or less than {nameof(bufferPrevState)} argument's length");

            var result = new List<MJpegFrameRange>();
            MJpegFrameRange currentMJpegFrame = null;
            var additionalBytes = new List<byte>();
            for (int i = 0; i < currentBufferBytesRead - 1; i++)
            {
                if (currentBuffer[i] == JpegStartPattern[0] && currentBuffer[i + 1] == JpegStartPattern[1])
                {
                    if (currentMJpegFrame != null)
                        throw new InvalidOperationException($"sequential mjpeg start patterns(without any end pattern) found in {nameof(currentBuffer)}");

                    currentMJpegFrame = new MJpegFrameRange { Start = i, End = currentBufferBytesRead - 1 };
                }
                else if (currentBuffer[i] == JpegEndPattern[0] && currentBuffer[i + 1] == JpegEndPattern[1])
                {
                    if (currentMJpegFrame == null)
                    {
                        if (result.Count > 0)
                            throw new InvalidOperationException($"mjpeg frame end pattern found without any start pattern in {nameof(currentBuffer)}");

                        currentMJpegFrame = new MJpegFrameRange { IsFinal = true, Start = 0, End = i + 1 };
                    }
                    else
                    {
                        currentMJpegFrame.End = i + 1;
                        currentMJpegFrame.IsFinal = true;
                    }

                    result.Add(currentMJpegFrame);
                    currentMJpegFrame = null;
                }
                // check case when part of start or end pattern were located in the end of previous buffer chunk
                else if (i == 0 && bufferPrevState != null)
                {
                    if (bufferPrevState[bufferPrevStateReadBytes - 1] == JpegStartPattern[0]
                        && currentBuffer[i] == JpegStartPattern[1])
                    {
                        additionalBytes.Add(bufferPrevState[bufferPrevStateReadBytes - 1]);
                        currentMJpegFrame = new MJpegFrameRange { Start = 0, End = currentBufferBytesRead - 1 };
                    }
                    else if (bufferPrevState[bufferPrevStateReadBytes - 1] == JpegEndPattern[0]
                        && currentBuffer[i] == JpegEndPattern[1])
                    {
                        currentMJpegFrame = new MJpegFrameRange { IsFinal = true, Start = 0, End = 0 };
                        result.Add(currentMJpegFrame);
                        currentMJpegFrame = null;
                    }
                }
            }

            if (currentMJpegFrame != null)
                result.Add(currentMJpegFrame);
            else if (result.Count == 0 && currentMJpegFrame == null)
                result.Add(new MJpegFrameRange { Start = 0, End = currentBufferBytesRead - 1 });

            return new KeyValuePair<List<MJpegFrameRange>, byte[]>(result, additionalBytes.ToArray());
        }

        public virtual async Task ReadStreamAsync(string urlOrPath, Action<Stream> callback, IStreamProvider streamProvider)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            using (MemoryStream mStream = new MemoryStream())
            {
                using (Stream stream = await streamProvider.GetStreamAsync(urlOrPath))
                {
                    byte[] buffer = new byte[BUFFER_SIZE];
                    byte[] prevBuffer = null;
                    int prevBufferReadBytes = 0;
                    var bytesRead = 0;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, BUFFER_SIZE)) > 0)
                    {
                        KeyValuePair<List<MJpegFrameRange>, byte[]> indexes = GetJpegStartAndEndPositions(buffer, bytesRead, prevBuffer, prevBufferReadBytes);
                        if (indexes.Value.Length > 0)
                            mStream.Write(indexes.Value, 0, indexes.Value.Length);

                        foreach (MJpegFrameRange mJpegFrameRange in indexes.Key)
                        {
                            mStream.Write(buffer, mJpegFrameRange.Start, mJpegFrameRange.Offset);
                            if (mJpegFrameRange.IsFinal)
                            {
                                mStream.Position = 0;
                                callback(mStream);
                                // clear memory stream
                                mStream.SetLength(0);
                            }
                        }

                        prevBuffer = buffer.ToArray();
                        prevBufferReadBytes = bytesRead;
                    }
                }
            }
        }
    }
}
