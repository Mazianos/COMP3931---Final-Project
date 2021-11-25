using System;
using System.Runtime.InteropServices;

namespace WaveAnalyzer
{
    public static class ModelessDialog
    {
        [DllImport("ModelessDialog.dll")] public static extern IntPtr GetSaveBuffer();
        [DllImport("ModelessDialog.dll")] public static extern uint GetDWDataLength();
        [DllImport("ModelessDialog.dll")] public static extern unsafe void SetSaveBuffer(byte* saveBuffer);
        [DllImport("ModelessDialog.dll")] public static extern void SetDWDataLength(uint dataWord);
        [DllImport("ModelessDialog.dll")] public static extern void InitWave();
        [DllImport("ModelessDialog.dll")] public static extern void BeginRecord();
        [DllImport("ModelessDialog.dll")] public static extern void EndRecord();
        [DllImport("ModelessDialog.dll")] public static extern void BeginPlay();
        [DllImport("ModelessDialog.dll")] public static extern void PausePlay();
        [DllImport("ModelessDialog.dll")] public static extern void EndPlay();
        [DllImport("ModelessDialog.dll")] public static extern void SetDWDataLength(ulong dataWord);
    }
}
