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
        /// This method defines when current frame should end, and another start, etc. 
        /// this method is public because, it need to be tested
        /// </summary>
        /// <param name="currentBuffer">current buffer</param>
        /// <param name="currentBufferBytesRead">current buffer bytes read</param>
        /// <param name="bufferPrevState">previous buffer state</param>
        /// <param name="bufferPrevStateReadBytes">previous buffer bytes read</param>
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
                // check that current and next byte in current buffer are belongs to JPEG frame start pattern(byte sequence)
                if (currentBuffer[i] == JpegStartPattern[0] && currentBuffer[i + 1] == JpegStartPattern[1])
                {
                    if (currentMJpegFrame != null)
                        throw new InvalidOperationException($"sequential mjpeg start patterns(without any end pattern) found in {nameof(currentBuffer)}");

                    currentMJpegFrame = new MJpegFrameRange { Start = i, End = currentBufferBytesRead - 1 };
                }
                // check that current and next byte in current buffer are belongs to JPEG frame end pattern(byte sequence)
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
            // in other case(where no any frame start or end pattern found) just consider that current buffer contains part of current frames data
            else if (result.Count == 0 && currentMJpegFrame == null)
                result.Add(new MJpegFrameRange { Start = 0, End = currentBufferBytesRead - 1 });

            return new KeyValuePair<List<MJpegFrameRange>, byte[]>(result, additionalBytes.ToArray());
        }

        /// <summary>
        /// Read stream and return image as stream in callback
        /// </summary>
        /// <param name="urlOrPath">Url or Path of MJPEG stream/file</param>
        /// <param name="callback">Callback that called after processing every frame of MJPEG</param>
        /// <param name="streamProvider">Stream Provider, in our case this is File or Web</param>
        /// <returns></returns>
        public async Task ReadStreamAsync(string urlOrPath, Action<Stream> callback, IStreamProvider streamProvider)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            // we have another options: using byte array, but i condidered to use Memory Stream to contains data of current MJPEG Frame
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
                        // define that current frame ended to another started, etc in GetJpegStartAndEndPositions function
                        KeyValuePair<List<MJpegFrameRange>, byte[]> indexes = GetJpegStartAndEndPositions(buffer, bytesRead, prevBuffer, prevBufferReadBytes);
                        if (indexes.Value.Length > 0)
                            mStream.Write(indexes.Value, 0, indexes.Value.Length);

                        foreach (MJpegFrameRange mJpegFrameRange in indexes.Key)
                        {
                            mStream.Write(buffer, mJpegFrameRange.Start, mJpegFrameRange.Offset);
                            // if there are end of current frame, just send callback and clear memory stream
                            if (mJpegFrameRange.IsFinal)
                            {
                                mStream.Position = 0;
                                callback(mStream);
                                // clear memory stream
                                mStream.SetLength(0);
                            }
                        }

                        // cache current buffer as prevBuffer
                        prevBuffer = buffer.ToArray();
                        prevBufferReadBytes = bytesRead;
                    }
                }
            }
        }
    }
}
