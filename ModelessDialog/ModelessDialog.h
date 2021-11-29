#pragma once

#include <Windows.h>

#define SOUND_PROCESSING_API __declspec(dllexport)



extern SOUND_PROCESSING_API PBYTE GetSaveBuffer();

extern SOUND_PROCESSING_API DWORD GetDWDataLength();

extern SOUND_PROCESSING_API void SetWaveData(PBYTE, DWORD, int, int, int, int);

extern SOUND_PROCESSING_API void ReverseMemoryFunct();

extern SOUND_PROCESSING_API void InitWave();

extern SOUND_PROCESSING_API void BeginRecord();

extern SOUND_PROCESSING_API void EndRecord();

extern SOUND_PROCESSING_API void PausePlay();

extern SOUND_PROCESSING_API void BeginPlay();

extern SOUND_PROCESSING_API void EndPlay();

extern SOUND_PROCESSING_API BOOL checkStopped();
