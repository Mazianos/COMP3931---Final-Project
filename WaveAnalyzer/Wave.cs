using Microsoft.Win32;
using System;
using System.IO;

namespace WaveAnalyzer
{
    class Wave
    {
        private int chunkID;
        private int chunkSize;
        private int format;
        private int subchunk1ID;
        private int subchunk1Size;
        private short audioFormat;
        public short NumChannels { get; private set; }
        public int SampleRate { get; private set; }
        private int byteRate;
        public short BlockAlign { get; private set; }
        public short BitsPerSample { get; private set; }
        private int subchunk2ID;
        public int Subchunk2Size { get; private set; }

        public short[][] Channels { get; private set; }
        private short dataIndex;

        private const short DefaultHeaderLength = 44;

        // Predetermined WAV header values.
        private const int RIFF = 0x52494646;
        private const int WAVE = 0x57415645;
        private const int fmt_ = 0x666d7420;
        private const int dataTag = 0x64617461;

        // Default wave values if a file is not provided.
        private const int defaultChunkSize = 49225302;
        private const int defaultSubchunk1Size = 16;
        private const short defaultAudioFormat = 1;
        private const int defaultNumChannels = 1;
        private const int defaultSampleRate = 44100;
        private const int defaultByteRate = 176400;
        private const short defaultBlockAlign = 2;
        private const short defaultBitsPerSample = 16;
        

        public Wave()
        {
            // Write "RIFF".
            chunkID = RIFF;
            chunkSize = defaultChunkSize;
            // Write "WAVE".
            format = WAVE;
            // Write "fmt ".
            subchunk1ID = fmt_;
            subchunk1Size = defaultSubchunk1Size;
            audioFormat = defaultAudioFormat;
            NumChannels = defaultNumChannels;
            SampleRate = defaultSampleRate;
            byteRate = defaultByteRate;
            BlockAlign = defaultBlockAlign;
            BitsPerSample = defaultBitsPerSample;
            // Write "data".
            subchunk2ID = dataTag;
            Subchunk2Size = 0;
            dataIndex = DefaultHeaderLength;

            short[][] channels = new short[NumChannels][];

            for (short i = 0; i < NumChannels; ++i)
            {
                channels[i] = new short[5];
            }

            Channels = channels;
        }

        public Wave(string filePath)
        {
            byte[] data = File.ReadAllBytes(filePath);

            InitializeWaveHeader(ref data);

            // Initialize the channels 2D array where each row is a channel and every column is a sample.
            int samplesPerChannel = Subchunk2Size / 2 / NumChannels;
            Channels = ExtractSamples(ref data, samplesPerChannel, dataIndex, NumChannels);
        }

        private void InitializeWaveHeader(ref byte[] data)
        {
            dataIndex = 0;

            // Read until it hits "RIFF".
            while (ByteConverter.ToInt32BigEndian(data, dataIndex) != RIFF)
            {
                ++dataIndex;
            }

            chunkID = ByteConverter.ToInt32BigEndian(data, dataIndex);
            dataIndex += 4;
            chunkSize = ByteConverter.ToInt32(data, dataIndex);
            dataIndex += 4;

            // Read until it hits "WAVE".
            while (ByteConverter.ToInt32BigEndian(data, dataIndex) != WAVE)
            {
                ++dataIndex;
            }

            format = ByteConverter.ToInt32BigEndian(data, dataIndex);
            dataIndex += 4;

            // Read until it hits "fmt ".
            while (ByteConverter.ToInt32BigEndian(data, dataIndex) != fmt_)
            {
                ++dataIndex;
            }

            subchunk1ID = ByteConverter.ToInt32BigEndian(data, dataIndex);
            subchunk1Size = ByteConverter.ToInt32(data, dataIndex + 4);
            audioFormat = ByteConverter.ToInt16(data, dataIndex + 8);
            NumChannels = ByteConverter.ToInt16(data, dataIndex + 10);
            SampleRate = ByteConverter.ToInt32(data, dataIndex + 12);
            byteRate = ByteConverter.ToInt32(data, dataIndex + 16);
            BlockAlign = ByteConverter.ToInt16(data, dataIndex + 20);
            BitsPerSample = ByteConverter.ToInt16(data, dataIndex + 22);
            dataIndex += 22;

            // Read until it hits "data".
            while (ByteConverter.ToInt32BigEndian(data, dataIndex) != dataTag)
            {
                ++dataIndex;
            }

            subchunk2ID = ByteConverter.ToInt32BigEndian(data, dataIndex);
            dataIndex += 4;
            Subchunk2Size = ByteConverter.ToInt32(data, dataIndex);
            dataIndex += 4;
        }

        private byte[] ConstructWaveHeader()
        {
            byte[] waveHeader = new byte[DefaultHeaderLength];

            Array.Copy(ByteConverter.ToBytesBigEndian(RIFF), 0, waveHeader, 0, 4);
            Array.Copy(BitConverter.GetBytes(chunkSize), 0, waveHeader, 4, 4);
            Array.Copy(ByteConverter.ToBytesBigEndian(WAVE), 0, waveHeader, 8, 4);
            Array.Copy(ByteConverter.ToBytesBigEndian(fmt_), 0, waveHeader, 12, 4);
            Array.Copy(BitConverter.GetBytes(subchunk1Size), 0, waveHeader, 16, 4);
            Array.Copy(BitConverter.GetBytes(audioFormat), 0, waveHeader, 20, 2);
            Array.Copy(BitConverter.GetBytes(NumChannels), 0, waveHeader, 22, 2);
            Array.Copy(BitConverter.GetBytes(SampleRate), 0, waveHeader, 24, 4);
            Array.Copy(BitConverter.GetBytes(byteRate), 0, waveHeader, 28, 4);
            Array.Copy(BitConverter.GetBytes(BlockAlign), 0, waveHeader, 32, 2);
            Array.Copy(BitConverter.GetBytes(BitsPerSample), 0, waveHeader, 34, 2);
            Array.Copy(ByteConverter.ToBytesBigEndian(dataTag), 0, waveHeader, 36, 4);
            Array.Copy(BitConverter.GetBytes(Subchunk2Size), 0, waveHeader, 40, 4);

            return waveHeader;
        }

        public void Save()
        {
            byte[] newFileBytes = new byte[Subchunk2Size + DefaultHeaderLength];
            Array.Copy(ConstructWaveHeader(), newFileBytes, DefaultHeaderLength);
            Array.Copy(GetChannelsInBytes(0), 0, newFileBytes, DefaultHeaderLength, Subchunk2Size);

            SaveFileDialog saveFileDialog = new SaveFileDialog();

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, newFileBytes);
            }
        }

        public static short[][] ExtractSamples(ref byte[] data, int samplesPerChannel, int dataStart, int numChannels)
        {
            short[][] channels = new short[numChannels][];

            for (short i = 0; i < numChannels; ++i)
            {
                channels[i] = new short[samplesPerChannel];
            }

            // Iterate through the samples and push the short values to their respective channel arrays.
            // For mono, samples are two bytes each. For stereo, it is four bytes, first two left, then two right.
            for (int i = 0; i < samplesPerChannel; ++i)
            {
                for (short j = 0; j < numChannels; ++j)
                {
                    channels[j][i] = ByteConverter.ToInt16(data, dataStart + i * 2 * numChannels + j * numChannels);
                }
            }

            return channels;
        }

        public byte[] GetChannelsInBytes(int startingSample)
        {
            int start = startingSample < 0 ? 0 : startingSample;

            byte[] byteData = new byte[Subchunk2Size - start * 2 * NumChannels];

            for (int i = 0; i < Channels[0].Length - start; ++i)
            {
                for (int j = 0; j < NumChannels; ++j)
                {
                    byte[] current = BitConverter.GetBytes(Channels[j][i + start]);
                    byteData[i * 2 * NumChannels + j * NumChannels] = current[0];
                    byteData[i * 2 * NumChannels + j * NumChannels + 1] = current[1];
                }
            }

            return byteData;
        }

        public bool IsMono()
        {
            return Channels.Length == 1;
        }

        public short[][] ExtractSamples(int firstIndex, int secondIndex)
        {
            int start = firstIndex < secondIndex ? firstIndex : secondIndex;
            int end = firstIndex < secondIndex ? secondIndex : firstIndex;

            if (Channels[0].Length == 0)
            {
                return new short[0][];
            }
            if (start < 0)
            {
                start = 0;
            }
            if (end < 0)
            {
                end = 0;
            }
            if (end >= Channels[0].Length)
            {
                end = Channels[0].Length - 1;
            }

            // Holds the extracted samples for each channel.
            short[][] extractedSamples = new short[NumChannels][];
            
            for (int i = 0; i < NumChannels; ++i)
            {
                int extractedSamplesLength = end - start + 1;
                
                // Extract the samples for this channel in this array.
                extractedSamples[i] = new short[extractedSamplesLength];

                // Holds the remaining samples in the channel, after the specified samples are extracted.
                short[] newChannel = new short[Channels[i].Length - extractedSamplesLength];
                
                // Put the first half of the unextracted samples from the original channel to the new channel.
                for (int j = 0; j < start; ++j)
                {
                    newChannel[j] = Channels[i][j];
                }

                // Extracted the samples from the original channel to the new array.
                for (int j = 0; j < extractedSamplesLength; ++j)
                {
                    extractedSamples[i][j] = Channels[i][start + j];
                }

                // Put the second half of the unextracted samples from the original channel to the new channel.
                for (int j = start; j < newChannel.Length; ++j)
                {
                    newChannel[j] = Channels[i][extractedSamplesLength + j];
                }

                Channels[i] = newChannel;
            }

            return extractedSamples;
        }

        public void InsertSamples(short[][] samples, int position)
        {
            if (samples == null) return;

            if (position > Channels[0].Length - 1)
            {
                position = Channels[0].Length - 1;
            }
            if (position < 0)
            {
                position = 0;
            }
            
            // If the inserted samples have less channels than the song,
            // insert empty channels into the sample array until they are equal.
            if (samples.Length < Channels.Length)
            {
                int oldLength = samples.Length;
                Array.Resize(ref samples, Channels.Length);

                for (int i = Channels.Length - oldLength; i < Channels.Length; ++i)
                {
                    samples[i] = new short[samples[0].Length];
                }
            }

            for (int i = 0; i < Channels.Length; ++i)
            {
                short[] newChannel = new short[Channels[i].Length + samples[i].Length];

                for (int j = 0; j < position; ++j)
                {
                    newChannel[j] = Channels[i][j];
                }

                for (int j = 0; j < samples[i].Length; ++j)
                {
                    newChannel[j + position] = samples[i][j];
                }

                for (int j = position; j < Channels[i].Length; ++j)
                {
                    newChannel[j + samples[i].Length] = Channels[i][j];
                }

                Channels[i] = newChannel;
            }

            Subchunk2Size = Channels[0].Length * 2 * NumChannels;
        }
    }
}
