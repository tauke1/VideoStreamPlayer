using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VideoStreamPlayer.HttpClients;
using VideoStreamPlayer.Models;

namespace VideoStreamPlayer.UnitTests
{
    [TestClass]
    public class StreamClientTests
    {
        [TestMethod]
        public void GetJpegStartAndEndPositions_InputByteArraysHaveDiffirentLength_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = new StreamClient();
            var buffer = new byte[100];
            var prevBufferState = new byte[101];
            var bufferBytesread = 100;
            var prevBufferStateByteRead = 100;

            // Act
            Action actMethod = () => client.GetJpegStartAndEndPositions(buffer, bufferBytesread, prevBufferState, prevBufferStateByteRead);

            // Assert
            Assert.ThrowsException<InvalidOperationException>(actMethod);
        }

        [TestMethod]
        public void GetJpegStartAndEndPositions_CurrentBufferBytesReadIsMoreThanCurrentBuffersLength_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = new StreamClient();
            var buffer = new byte[100];
            var prevBufferState = new byte[100];
            var bufferBytesread = 101;
            var prevBufferStateByteRead = 100;

            // Act
            Action actMethod = () => client.GetJpegStartAndEndPositions(buffer, bufferBytesread, prevBufferState, prevBufferStateByteRead);

            // Assert
            Assert.ThrowsException<InvalidOperationException>(actMethod);
        }

        [TestMethod]
        public void GetJpegStartAndEndPositions_NullCurrentBufferPassed_ThrowsArgumentNullException()
        {
            // Arrange
            var client = new StreamClient();
            byte[] buffer = null;
            var prevBufferState = new byte[100];
            var bufferBytesread = 100;
            var prevBufferStateByteRead = 100;

            // Act
            Action actMethod = () => client.GetJpegStartAndEndPositions(buffer, bufferBytesread, prevBufferState, prevBufferStateByteRead);

            // Assert
            Assert.ThrowsException<ArgumentNullException>(actMethod);
        }

        [TestMethod]
        public void GetJpegStartAndEndPositions_BufferPrevStateBytesReadIsMoreThanBufferPrevStateLength_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = new StreamClient();
            var buffer = new byte[100];
            var prevBufferState = new byte[100];
            var bufferBytesread = 100;
            var prevBufferStateByteRead = 101;

            // Act
            Action actMethod = () => client.GetJpegStartAndEndPositions(buffer, bufferBytesread, prevBufferState, prevBufferStateByteRead);

            // Assert
            Assert.ThrowsException<InvalidOperationException>(actMethod);
        }

        [TestMethod]
        public void GetJpegStartAndEndPositions_JPEGFrameStartPatternStartsInBufferPrevStateAndEndsInCurrentBuffer_ReturnsListWithOneElement()
        {
            // Arrange
            var client = new StreamClient();
            var buffer = new byte[] { 216, 2, 3, 4, 5, 6, 7, 8, 255, 217 };
            var prevBufferState = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 255 };
            int bufferBytesread = 10;
            int prevBufferStateByteRead = 10;
            var expectedList = new List<MJpegFrameRange> { new MJpegFrameRange { Start = 0, End = 9, IsFinal = true } };
            var expectedAdditionalBytes = new byte[] { 255 };

            // Act
            KeyValuePair<List<MJpegFrameRange>, byte[]> frameRanges = client.GetJpegStartAndEndPositions(buffer, bufferBytesread, prevBufferState, prevBufferStateByteRead);

            // Assert
            CollectionAssert.AreEqual(expectedList, frameRanges.Key);
            CollectionAssert.AreEqual(expectedAdditionalBytes, frameRanges.Value);
        }

        [TestMethod]
        public void GetJpegStartAndEndPositions_JPEGFrameEndPatternStartsInBufferPrevStateAndEndsInCurrentBuffer_ReturnsListWithTwoElements()
        {
            // Arrange
            var client = new StreamClient();
            var buffer = new byte[] { 217, 2, 3, 255, 216, 6, 7, 8, 9, 10 };
            var prevBufferState = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 255 };
            int bufferBytesread = 10;
            int prevBufferStateByteRead = 10;
            var expectedList = new List<MJpegFrameRange> { new MJpegFrameRange { Start = 0, End = 0, IsFinal = true }, new MJpegFrameRange { Start = 3, End = 9, IsFinal = false } };
            var expectedAdditionalBytes = new byte[0];

            // Act
            KeyValuePair<List<MJpegFrameRange>, byte[]> frameRanges = client.GetJpegStartAndEndPositions(buffer, bufferBytesread, prevBufferState, prevBufferStateByteRead);

            // Assert
            CollectionAssert.AreEqual(expectedList, frameRanges.Key);
            CollectionAssert.AreEqual(expectedAdditionalBytes, frameRanges.Value);
        }

        [TestMethod]
        public void GetJpegStartAndEndPositions_CurrentBufferContains2FramesAndBufferPrevStateIsNull_ReturnsListWithTwoElements()
        {
            // Arrange
            var client = new StreamClient();
            var buffer = new byte[] { 255, 215, 3, 255, 217, 6, 255, 216, 9, 10, 255, 217 };
            int bufferBytesread = 12;
            var expectedList = new List<MJpegFrameRange> { new MJpegFrameRange { Start = 0, End = 4, IsFinal = true }, new MJpegFrameRange { Start = 6, End = 11, IsFinal = true } };
            var expectedAdditionalBytes = new byte[0];

            // Act
            KeyValuePair<List<MJpegFrameRange>, byte[]> frameRanges = client.GetJpegStartAndEndPositions(buffer, bufferBytesread, null, 0);

            // Assert
            CollectionAssert.AreEqual(expectedList, frameRanges.Key);
            CollectionAssert.AreEqual(expectedAdditionalBytes, frameRanges.Value);
        }
    }
}
