using System;
using System.Runtime.InteropServices;

namespace WaveAnalyzer
{
    public static class ModelessDialog
    {
        [DllImport("ModelessDialog.dll")] public static extern IntPtr GetSaveBuffer();
        [DllImport("ModelessDialog.dll")] public static extern uint GetDWDataLength();
        [DllImport("ModelessDialog.dll")] public static extern unsafe void SetWaveData(byte* saveBuffer, uint length, int numChannels, int sampleRate, int blockAlign, int bitsPerSample);
        [DllImport("ModelessDialog.dll")] public static extern void InitWave();
        [DllImport("ModelessDialog.dll")] public static extern void BeginRecord();
        [DllImport("ModelessDialog.dll")] public static extern void EndRecord();
        [DllImport("ModelessDialog.dll")] public static extern void BeginPlay();
        [DllImport("ModelessDialog.dll")] public static extern void PausePlay();
        [DllImport("ModelessDialog.dll")] public static extern void EndPlay();
        [DllImport("ModelessDialog.dll")] public static extern bool checkStopped();
        [DllImport("ModelessDialog.dll")] public static extern void setStopped(bool stopped);
    }
}
