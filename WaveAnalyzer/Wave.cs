using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        public int SampleRate { get; set; }
        private int byteRate;
        public short BlockAlign { get; private set; }
        public short BitsPerSample { get; private set; }
        private int subchunk2ID;
        public int Subchunk2Size { get; private set; }

        public short[][] Channels { get; set; }
        private short dataIndex;

        private const short DefaultHeaderLength = 44;
        private const byte key = 254;

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
            System.Diagnostics.Trace.WriteLine(string.Join(",", Encode(new byte[] { 0, 1, 2, 5, 3, 0, 0, 1, 2, 3, 5, 5, })));


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
                channels[i] = new short[0];
            }

            Channels = channels;
        }

        public Wave(Wave other)
        {
            chunkID = other.chunkID;
            chunkSize = other.chunkSize;
            format = other.format;
            subchunk1ID = other.subchunk1ID;
            subchunk1Size = other.subchunk1Size;
            audioFormat = other.audioFormat;
            NumChannels = other.NumChannels;
            SampleRate = other.SampleRate;
            byteRate = other.byteRate;
            BlockAlign = other.BlockAlign;
            BitsPerSample = other.BitsPerSample;
            subchunk2ID = other.subchunk2ID;
            Subchunk2Size = other.Subchunk2Size;
        }

        public Wave(string filePath)
        {
            byte[] data = File.ReadAllBytes(filePath);

            InitializeWaveHeader(ref data);

            if (Path.GetExtension(filePath) == ".rmle")
            {
                data = Unencode(ref data);
            }

            // Initialize the channels 2D array where each row is a channel and every column is a sample.
            int samplesPerChannel = Subchunk2Size / 2 / NumChannels;
            Channels = GetChannelsFromBytes(ref data, samplesPerChannel, dataIndex, NumChannels);
        }

        /*private byte[] UnencodeRMLE(ref byte[] data)
        {
            List<byte> unencoded = new List<byte>();

            byte[] header = new byte[dataIndex];
            Array.Copy(data, header, dataIndex);

            unencoded.AddRange(header);

            byte currentByte = data[dataIndex];

            for (int i = dataIndex + 1; i < data.Length; ++i)
            {
                if (currentByte == key)
                {
                    if (data.Length - 3 < i) break;
                    for (int j = 0; j < data[i + 1]; ++j)
                    {
                        unencoded.Add(data[i + 2]);
                    }
                    i += 2;
                }
                else
                {
                    unencoded.Add(data[i]);
                }
            }

            return unencoded.ToArray();
        }*/

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

        const uint MaxPatternLength = 10000;

        private byte[] Encode(byte[] bytes)
        {
            List<byte> encode = new List<byte>();

            int occurrences = 1;
            int lastSequenceStart = 0;

            void AddOccurrences(int index, int length)
            {
                lastSequenceStart = index + (length * occurrences);
                while (occurrences > 0)
                {
                    encode.Add(occurrences > 255 ? (byte)255 : (byte)occurrences);

                    int lengthCopy = length;
                    while (lengthCopy > 0)
                    {
                        encode.Add(lengthCopy > 255 ? (byte)255 : (byte)lengthCopy);
                        lengthCopy -= 255;
                    }
                    encode.Add(0);

                    for (int i = index; i < index + length; ++i)
                    {
                        encode.Add(bytes[i]);
                    }

                    occurrences -= 255;
                }
                occurrences = 1;
            }

            for (int i = 0; i < bytes.Length; ++i)
            {
                bool foundPattern = false;
                for (int j = i + 1; j < bytes.Length && j < i + MaxPatternLength; ++j)
                {
                    int length = 0;
                    foundPattern = false;

                    // This is where occurences of a certain pattern stack up, if one can be found.
                    while (j < bytes.Length && bytes[i] == bytes[j])
                    {
                        int patternLength = foundPattern ? length : j - i;

                        if (j + patternLength - 1 >= bytes.Length) break;
                        if (!foundPattern)
                        {
                            length = j - i;
                            foundPattern = true;
                        }

                        ++occurrences;
                        j += length;
                    }

                    if (foundPattern)
                    {
                        AddOccurrences(i, length);
                        i = j - 1;
                        break;
                    }
                }

                if (!foundPattern)
                {
                    AddOccurrences(i, 1);
                }
            }

            int lastSequenceLength = bytes.Length - lastSequenceStart;
            if (lastSequenceLength > 0)
            {
                AddOccurrences(lastSequenceStart, lastSequenceLength);
            }

            return encode.ToArray();
        }

        private byte[] Unencode(ref byte[] data)
        {
            List<byte> unencoded = new List<byte>();

            byte[] header = new byte[dataIndex];
            Array.Copy(data, header, dataIndex);

            unencoded.AddRange(header);

            int index = dataIndex;

            while (index < data.Length)
            {
                int occurrences = data[index++];
                int length = 0;

                while (data[index] != 0)
                {
                    length += data[index++];
                }

                ++index;

                for (int i = 0; i < occurrences; ++i)
                {
                    for (int j = 0; j < length; ++j)
                    {
                        unencoded.Add(data[index + j]);
                    }
                }

                index += length;
            }

            return unencoded.ToArray();
        }

        private byte[] ModifiedRunLengthEncode(byte[] bytes)
        {
            List<byte> encode = new List<byte>();

            int occurrences = 1;
            bool run = false;

            void AddOccurrences(byte value)
            {
                if (run)
                {
                    while (occurrences > 255)
                    {
                        encode.Add(key);
                        encode.Add(255);
                        encode.Add(value);
                        occurrences -= 255;
                    }
                    encode.Add(key);
                    encode.Add((byte)occurrences);
                    encode.Add(value);

                    run = false;
                }
                else
                {
                    for (int j = 0; j < occurrences; ++j)
                    {
                        encode.Add(value);
                    }
                }
            }

            for (int i = 1; i < bytes.Length; ++i)
            {
                if (bytes[i - 1] == key || occurrences >= 3)
                {
                    run = true;
                }

                if (bytes[i] == bytes[i - 1] && i < bytes.Length - 1)
                {
                    ++occurrences;
                }
                else
                {
                    AddOccurrences(bytes[i - 1]);

                    occurrences = 1;
                }
            }

            AddOccurrences(bytes[bytes.Length - 1]);

            return encode.ToArray();
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
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                DefaultExt = ".rmle",
                Filter = "Modified RLE file|*.rmle|WAV files (*.wav)|*.wav|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                byte[] data;
                if (Path.GetExtension(saveFileDialog.FileName) == "." + saveFileDialog.DefaultExt) {
                    data = Encode(GetBytesFromChannels(0));
                }
                else
                {
                    data = GetBytesFromChannels(0);
                }
                byte[] newFileBytes = new byte[data.Length + DefaultHeaderLength];

                Array.Copy(data, 0, newFileBytes, DefaultHeaderLength, data.Length);
                Array.Copy(ConstructWaveHeader(), newFileBytes, DefaultHeaderLength);

                File.WriteAllBytes(saveFileDialog.FileName, newFileBytes);
            }
        }

        public static short[][] GetChannelsFromBytes(ref byte[] data, int samplesPerChannel, int dataStart, int numChannels)
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

        public byte[] GetBytesFromChannels(int startingSample)
        {
            int start = Math.Max(0, startingSample);

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

        public short[][] ExtractSamples(int firstIndex, int secondIndex, bool permanentlyModify)
        {
            int start = Math.Min(firstIndex, secondIndex);
            int end = Math.Max(firstIndex, secondIndex);

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
                
                // Extracted the samples from the original channel to the new array.
                for (int j = 0; j < extractedSamplesLength; ++j)
                {
                    extractedSamples[i][j] = Channels[i][start + j];
                }

                // Continue if not permanently modifying (copy, not cut).
                if (!permanentlyModify) continue;

                // Holds the remaining samples in the channel, after the specified samples are extracted.
                short[] newChannel = new short[Channels[i].Length - extractedSamplesLength];

                // Put the first half of the unextracted samples from the original channel to the new channel.
                for (int j = 0; j < start; ++j)
                {
                    newChannel[j] = Channels[i][j];
                }

                // Put the second half of the unextracted samples from the original channel to the new channel.
                for (int j = start; j < newChannel.Length; ++j)
                {
                    newChannel[j] = Channels[i][extractedSamplesLength + j];
                }

                Channels[i] = newChannel;
            }

            Subchunk2Size = Channels[0].Length * 2 * NumChannels;

            return extractedSamples;
        }

        public void InsertSamples(Wave wave, int position)
        {
            if (wave == null) return;

            if (position > Channels[0].Length - 1)
            {
                position = Channels[0].Length - 1;
            }
            if (position < 0)
            {
                position = 0;
            }

            short[][] samples = new short[wave.Channels.Length][];
            for (int i = 0; i < samples.Length; ++i)
            {
                samples[i] = new short[wave.Channels[i].Length];
                Array.Copy(wave.Channels[i], samples[i], wave.Channels[i].Length);
            }

            int otherSampleRate = wave.SampleRate;

            while (otherSampleRate - SampleRate < -1000)
            {
                samples = UpSample(samples);

                otherSampleRate <<= 1;
            }

            while (otherSampleRate - SampleRate > 1000)
            {
                samples = DownSample(samples);

                otherSampleRate >>= 1;
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

        private short[][] UpSample(short[][] samples)
        {
            short[][] upSamples = new short[samples.Length][];

            for (int i = 0; i < upSamples.Length; ++i)
            {
                upSamples[i] = new short[samples[i].Length * 2];
                
                for (int j = 0; j < samples[i].Length; ++j)
                {
                    upSamples[i][j * 2] = samples[i][j];
                    upSamples[i][j * 2 + 1] = samples[i][j];
                }
            }

            return upSamples;
        }

        private short[][] DownSample(short[][] samples)
        {
            short[][] downSamples = new short[samples.Length][];

            for (int i = 0; i < downSamples.Length; ++i)
            {
                downSamples[i] = new short[samples[i].Length / 2];

                for (int j = 0; j < downSamples[i].Length; ++j)
                {
                    downSamples[i][j] = samples[i][j * 2];
                }
            }

            return downSamples;
        }
    }
}
