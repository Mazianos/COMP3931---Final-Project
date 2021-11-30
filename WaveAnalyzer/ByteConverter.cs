namespace WaveAnalyzer
{
    public static class ByteConverter
    {
        public static short ToInt16(byte[] buffer, int i) => (short)(buffer[i] | (buffer[i + 1] << 8));

        public static int ToInt32(byte[] buffer, int i) => buffer[i] | (buffer[i + 1] << 8) | (buffer[i + 2] << 16) | (buffer[i + 3] << 24);

        public static int ToInt32BigEndian(byte[] buffer, int i) => (buffer[i] << 24) | (buffer[i + 1] << 16) | (buffer[i + 2] << 8) | buffer[i + 3];

        public static byte[] ToBytesBigEndian(int value) => new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value) };
    }
}
