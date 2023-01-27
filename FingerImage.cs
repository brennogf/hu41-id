using ControliD;

namespace CaptureExample
{
    internal class FingerImage
    {
        public RetCode ret { get; set; }
        public byte[] imageBuf { get; set; }
        public uint width { get; set; }
        public uint height { get; set; }
    }
}