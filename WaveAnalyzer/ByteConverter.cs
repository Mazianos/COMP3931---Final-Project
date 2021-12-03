namespace WaveAnalyzer
{
    // Used to convert bytes to 16-bit integers, 32-bit integers, 32-bit integers from big endian, and to bytes in big endian
    public static class ByteConverter
    {
        /// <summary>
        /// Convert byte to 16-bit integer
        /// </summary>
        /// <param name="buffer">Byte array</param>
        /// <param name="i">Index of byte to convert</param>
        /// <returns>Byte as a short</returns>
        public static short ToInt16(byte[] buffer, int i) => (short)(buffer[i] | (buffer[i + 1] << 8));

        /// <summary>
        /// Convert byte to 32-bit integer
        /// </summary>
        /// <param name="buffer">Byte array</param>
        /// <param name="i">Index of byte to convert</param>
        /// <returns>Byte as an int</returns>
        public static int ToInt32(byte[] buffer, int i) => buffer[i] | (buffer[i + 1] << 8) | (buffer[i + 2] << 16) | (buffer[i + 3] << 24);

        /// <summary>
        /// Convert big endian byte to 32-bit integer
        /// </summary>
        /// <param name="buffer">Byte array</param>
        /// <param name="i">Index of byte to convert</param>
        /// <returns>Byte as an int</returns>
        public static int ToInt32BigEndian(byte[] buffer, int i) => (buffer[i] << 24) | (buffer[i + 1] << 16) | (buffer[i + 2] << 8) | buffer[i + 3];

        /// <summary>
        /// Convert an int to a big-endian byte
        /// </summary>
        /// <param name="value">Integer we are converting</param>
        /// <returns>Big endian byte array</returns>
        public static byte[] ToBytesBigEndian(int value) => new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value) };
    }
}
