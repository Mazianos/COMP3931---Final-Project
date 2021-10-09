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
        private short[] leftChannel;
        private short[] rightChannel;

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
            // The number of samples in the left channel is halved if stereo.
            leftChannel = numChannels != 1 ? new short[subchunk2Size / 2] : new short[subchunk2Size];
            // rightChannel (stereo) will only be needed if the number of channels is not 1 (mono).
            rightChannel = numChannels != 1 ? new short[subchunk2Size / 2] : null;

            // Start at the data index to get the samples.
            int byteIndex = DATA_INDEX;

            // Iterate through the samples and push the float values to their respective channel lists.
            // For mono, samples are two bytes each. For stereo, it is four bytes, first two left, then two right.
            int leftChannelIndex = 0;
            int rightChannelIndex = 0;

            while (byteIndex < subchunk2Size)
            {
                leftChannel[leftChannelIndex++] = ByteConverter.ToInt16(data, byteIndex);
                byteIndex += 2;

                if (rightChannel != null)
                {
                    rightChannel[rightChannelIndex++] = ByteConverter.ToInt16(data, byteIndex);
                    byteIndex += 2;
                }
            }
        }

        public short[] GetLeftChannel()
        {
            return leftChannel;
        }

        public short[] GetRightChannel()
        {
            return rightChannel;
        }
    }
}
