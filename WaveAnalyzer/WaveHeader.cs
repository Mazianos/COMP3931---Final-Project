namespace WaveAnalyzer
{
    public struct WaveHeader
    {
        public int chunkID;
        public int chunkSize;
        public int format;
        public int subchunk1ID;
        public int subchunk1Size;
        public short audioFormat;
        public short numChannels;
        public int sampleRate;
        public int byteRate;
        public short blockAlign;
        public short bitsPerSample;
        public int subchunk2ID;
        public int subchunk2Size;
    }
}
