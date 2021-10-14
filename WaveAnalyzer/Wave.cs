using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WaveAnalyzer
{
    class Wave
    {
        private const int DATA_INDEX = 44;
        private int chunkID;
        private int chunkSize;
        private int format;
        private int subchunk1ID;
        private int subchunk1Size;
        private short audioFormat;
        private short numChannels;
        private int sampleRate;
        private int byteRate;
        private short blockAlign;
        private short bitsPerSample;
        private int subchunk2ID;
        private int subchunk2Size;
        private short[][] channels;

        public Wave(string filePath)
        {
            byte[] data = File.ReadAllBytes(filePath);

            chunkID = ByteConverter.ToInt32BigEndian(data, 0);
            chunkSize = ByteConverter.ToInt32(data, 4);
            format = ByteConverter.ToInt32BigEndian(data, 8);
            subchunk1ID = ByteConverter.ToInt32BigEndian(data, 12);
            subchunk1Size = ByteConverter.ToInt32(data, 16);
            audioFormat = ByteConverter.ToInt16(data, 20);
            numChannels = ByteConverter.ToInt16(data, 22);
            sampleRate = ByteConverter.ToInt32(data, 24);
            byteRate = ByteConverter.ToInt32(data, 28);
            blockAlign = ByteConverter.ToInt16(data, 32);
            bitsPerSample = ByteConverter.ToInt16(data, 34);
            subchunk2ID = ByteConverter.ToInt32BigEndian(data, 36);
            subchunk2Size = ByteConverter.ToInt32(data, 40);

            ExtractSamples(data);
        }

        private void ExtractSamples(byte[] data)
        {
            // Initialize the channels 2D array where each row is a channel and every column is a sample.
            channels = new short[numChannels][];
            int samplesPerChannel = subchunk2Size / 2 / numChannels;
            for (short i = 0; i < numChannels; ++i)
            {
                channels[i] = new short[samplesPerChannel];
            }

            // Iterate through the samples and push the float values to their respective channel arrays.
            // For mono, samples are two bytes each. For stereo, it is four bytes, first two left, then two right.
            for (int i = 0; i < samplesPerChannel; ++i)
            {
                for (short j = 0; j < numChannels; ++j)
                {
                    channels[j][i] = ByteConverter.ToInt16(data, DATA_INDEX + (i * 2 * numChannels) + (j * numChannels));
                }
            }
        }

        public short[][] GetChannels()
        {
            return channels;
        }

        public bool IsMono()
        {
            return channels.Length == 1;
        }

        public short[][] ExtractSamples(int firstIndex, int secondIndex)
        {
            int start = firstIndex < secondIndex ? firstIndex : secondIndex;
            int end = firstIndex < secondIndex ? secondIndex : firstIndex;

            if (channels[0].Length == 0)
            {
                return new short[0][];
            }
            if (start < 0)
            {
                start = 0;
            }
            if (end > channels[0].Length - 1)
            {
                end = channels[0].Length - 1;
            }

            // Holds the extracted samples for each channel.
            short[][] extractedSamples = new short[numChannels][];
            
            for (int i = 0; i < numChannels; ++i)
            {
                int extractedSamplesLength = end - start + 1;
                // Extract the samples for this channel in this array.
                extractedSamples[i] = new short[extractedSamplesLength];

                // Holds the remaining samples in the channel, after the specified samples are extracted.
                short[] newChannel = new short[channels[i].Length - extractedSamplesLength];
                
                // Put the first half of the unextracted samples from the original channel to the new channel.
                for (int j = 0; j < start; ++j)
                {
                    newChannel[j] = channels[i][j];
                }

                // Extracted the samples from the original channel to the new array.
                for (int j = 0; j < extractedSamplesLength; ++j)
                {
                    extractedSamples[i][j] = channels[i][start + j];
                }

                // Put the second half of the unextracted samples from the original channel to the new channel.
                for (int j = start; j < newChannel.Length; ++j)
                {
                    newChannel[j] = channels[i][extractedSamplesLength + j];
                }

                channels[i] = newChannel;
            }

            return extractedSamples;
        }

        public void InsertSamples(short[][] samples, int position)
        {
            if (samples == null)
            {
                return;
            }
            if (position > channels[0].Length - 1)
            {
                position = channels[0].Length - 1;
            }
            if (position < 0)
            {
                position = 0;
            }

            for (int i = 0; i < numChannels; ++i)
            {
                short[] newChannel = new short[channels[i].Length + samples[i].Length];

                for (int j = 0; j < position; ++j)
                {
                    newChannel[j] = channels[i][j];
                }

                for (int j = 0; j < samples[i].Length; ++j)
                {
                    newChannel[j + position] = samples[i][j];
                }

                for (int j = position; j < channels[i].Length; ++j)
                {
                    newChannel[j + samples[i].Length] = channels[i][j];
                }

                channels[i] = newChannel;
            }
        }

        public unsafe byte* GetMonoData()
        {
            byte[] data = new byte[subchunk2Size];
            int byteIndex = 0, leftChannelIndex = 0, rightChannelIndex = 0;
            while (byteIndex < subchunk2Size)
            {
                data[byteIndex] = (byte) leftChannel[leftChannelIndex++];
                byteIndex += 2;

                if (rightChannel != null)
                {
                    data[byteIndex] = (byte) rightChannel[rightChannelIndex++];
                    byteIndex += 2;
                }
            }
            return data;
        }
    }
}
