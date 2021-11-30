namespace WaveAnalyzer
{
    public static class ByteConverter
    {
        public static short ToInt16(byte[] buffer, int i) => (short)(buffer[i] | (buffer[i + 1] << 8));

        public static int ToInt32(byte[] buffer, int i) => buffer[i] | (buffer[i + 1] << 8) | (buffer[i + 2] << 16) | (buffer[i + 3] << 24);

        public static int ToInt32BigEndian(byte[] buffer, int i) => (buffer[i] << 24) | (buffer[i + 1] << 16) | (buffer[i + 2] << 8) | buffer[i + 3];
    }
}
