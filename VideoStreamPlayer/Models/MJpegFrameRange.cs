using System;

namespace VideoStreamPlayer.Models
{
    public class MJpegFrameRange : IEquatable<MJpegFrameRange>
    {
        public int Start { get; set; }
        public int End { get; set; }
        public bool IsFinal { get; set; }

        public int Offset { get { return End - Start + 1; } }

        public bool Equals(MJpegFrameRange mJpegFrameRange)
        {
            if (mJpegFrameRange == null)
                return false;

            bool equals = this.Start == mJpegFrameRange.Start && this.End == mJpegFrameRange.End
                && this.IsFinal == mJpegFrameRange.IsFinal;

            return equals;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is MJpegFrameRange))
                return false;

            MJpegFrameRange unboxed = (MJpegFrameRange)obj;

            return Equals(unboxed);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = this.Start.GetHashCode();
                hashCode = (hashCode * 397) ^ this.End.GetHashCode();
                hashCode = (hashCode * 397) ^ this.IsFinal.GetHashCode();
                return hashCode;
            }
        }

    }
}
