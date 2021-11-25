using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WaveAnalyzer
{
    class Wave
    {
        private short dataIndex;
        private int chunkID;
        private int chunkSize;
        private int format;
        private int subchunk1ID;
        private int subchunk1Size;
        private short audioFormat;
        public short NumChannels { get; private set; }
        private int sampleRate;
        private int byteRate;
        private short blockAlign;
        private short bitsPerSample;
        private int subchunk2ID;
        public int Subchunk2Size { get; private set; }
        public short[][] Channels { get; private set; }

        public Wave(string filePath)
        {
            byte[] data = File.ReadAllBytes(filePath);

            InitializeWaveHeader(ref data);

            ExtractSamples(ref data);
        }

        private void InitializeWaveHeader(ref byte[] data)
        {
            dataIndex = 0;

            // Read until it hits "RIFF".
            while (ByteConverter.ToInt32BigEndian(data, dataIndex) != 0x52494646)
            {
                ++dataIndex;
            }

            chunkID = ByteConverter.ToInt32BigEndian(data, dataIndex);
            dataIndex += 4;
            chunkSize = ByteConverter.ToInt32(data, dataIndex);
            dataIndex += 4;

            // Read until it hits "WAVE".
            while (ByteConverter.ToInt32BigEndian(data, dataIndex) != 0x57415645)
            {
                ++dataIndex;
            }

            format = ByteConverter.ToInt32BigEndian(data, dataIndex);
            dataIndex += 4;

            // Read until it hits "fmt ".
            while (ByteConverter.ToInt32BigEndian(data, dataIndex) != 0x666d7420)
            {
                ++dataIndex;
            }

            subchunk1ID = ByteConverter.ToInt32BigEndian(data, dataIndex);
            subchunk1Size = ByteConverter.ToInt32(data, dataIndex + 4);
            audioFormat = ByteConverter.ToInt16(data, dataIndex + 8);
            NumChannels = ByteConverter.ToInt16(data, dataIndex + 10);
            sampleRate = ByteConverter.ToInt32(data, dataIndex + 12);
            byteRate = ByteConverter.ToInt32(data, dataIndex + 16);
            blockAlign = ByteConverter.ToInt16(data, dataIndex + 20);
            bitsPerSample = ByteConverter.ToInt16(data, dataIndex + 22);
            dataIndex += 22;

            // Read until it hits "data".
            while (ByteConverter.ToInt32BigEndian(data, dataIndex) != 0x64617461)
            {
                ++dataIndex;
            }

            subchunk2ID = ByteConverter.ToInt32BigEndian(data, dataIndex);
            dataIndex += 4;
            Subchunk2Size = ByteConverter.ToInt32(data, dataIndex);
            dataIndex += 4;
        }

        private void ExtractSamples(ref byte[] data)
        {
            // Initialize the channels 2D array where each row is a channel and every column is a sample.
            Channels = new short[NumChannels][];
            int samplesPerChannel = Subchunk2Size / 2 / NumChannels;
            for (short i = 0; i < NumChannels; ++i)
            {
                Channels[i] = new short[samplesPerChannel];
            }

            // Iterate through the samples and push the float values to their respective channel arrays.
            // For mono, samples are two bytes each. For stereo, it is four bytes, first two left, then two right.
            for (int i = 0; i < samplesPerChannel; ++i)
            {
                for (short j = 0; j < NumChannels; ++j)
                {
                    Channels[j][i] = ByteConverter.ToInt16(data, dataIndex + (i * 2 * NumChannels) + (j * NumChannels));
                }
            }
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
            if (end > Channels[0].Length - 1)
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
            if (samples == null)
            {
                return;
            }
            if (position > Channels[0].Length - 1)
            {
                position = Channels[0].Length - 1;
            }
            if (position < 0)
            {
                position = 0;
            }

            for (int i = 0; i < NumChannels; ++i)
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
        }
    }
}
