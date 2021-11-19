#pragma once

#define SOUND_PROCESSING_API __declspec(dllexport)

#include <Windows.h>

extern SOUND_PROCESSING_API PBYTE GetSaveBuffer();

extern SOUND_PROCESSING_API DWORD GetDWDataLength();

extern SOUND_PROCESSING_API void SetSaveBuffer(PBYTE);

extern SOUND_PROCESSING_API void SetDWDataLength(DWORD);

extern SOUND_PROCESSING_API void ReverseMemory(BYTE*, int);

extern SOUND_PROCESSING_API void InitWave();

extern SOUND_PROCESSING_API void BeginRecord();

extern SOUND_PROCESSING_API void EndRecord();

extern SOUND_PROCESSING_API void PausePlay();

extern SOUND_PROCESSING_API void BeginPlay();

extern SOUND_PROCESSING_API void EndPlay();
