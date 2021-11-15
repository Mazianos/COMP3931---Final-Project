// SOUND_PROCESSING.cpp : Defines the exported functions for the DLL.
//

#include <Windows.h>
#include "pch.h"
#include "framework.h"
#include "SoundProcessing.h"

#define IDC_RECORD_BEG                  1000
#define IDC_RECORD_END                  1001
#define IDC_PLAY_BEG                    1002
#define IDC_PLAY_PAUSE                  1003
#define IDC_PLAY_END                    1004
#define IDC_PLAY_REV                    1005
#define IDC_PLAY_REP                    1006
#define IDC_PLAY_SPEED                  1007
#define INP_BUFFER_SIZE                 16384

TCHAR szAppName[] = TEXT("Record1");
static WAVEFORMATEX waveform;
static BOOL         bRecording, bPlaying, bReverse, bPaused,
bEnding, bTerminating;
static DWORD        dwDataLength, dwRepetitions = 1;
static HWAVEIN      hWaveIn;
static HWAVEOUT     hWaveOut;
static PBYTE        pBuffer1, pBuffer2, pSaveBuffer, pNewBuffer;
static PWAVEHDR     pWaveHdr1, pWaveHdr2;
static TCHAR        szOpenError[] = TEXT("Error opening waveform audio!");
static TCHAR        szMemError[] = TEXT("Error allocating memory!");

static HINSTANCE hInstance;

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    hInstance = hModule;
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}


SOUND_PROCESSING_API PBYTE GetSaveBuffer() {
    return pSaveBuffer;
}

SOUND_PROCESSING_API DWORD GetDWDataLength() {
    return dwDataLength;
}

SOUND_PROCESSING_API void SetSaveBuffer(PBYTE newSaveBuffer) {
    pSaveBuffer = newSaveBuffer;
}

SOUND_PROCESSING_API void SetDWDataLength(DWORD newDWDataLength) {
    dwDataLength = newDWDataLength;
}

SOUND_PROCESSING_API void ReverseMemory(BYTE* pBuffer, int iLength)
{
    BYTE b;
    int  i;

    for (i = 0; i < iLength / 2; i++)
    {
        b = pBuffer[i];
        pBuffer[i] = pBuffer[iLength - i - 1];
        pBuffer[iLength - i - 1] = b;
    }
}

SOUND_PROCESSING_API void InitWave() {

    // Allocate memory for wave header

    pWaveHdr1 = malloc(sizeof(WAVEHDR));
    pWaveHdr2 = malloc(sizeof(WAVEHDR));

    // Allocate memory for save buffer

    pSaveBuffer = malloc(1);
}

SOUND_PROCESSING_API void BeginRecord() {
    // Allocate buffer memory

    pBuffer1 = malloc(INP_BUFFER_SIZE);
    pBuffer2 = malloc(INP_BUFFER_SIZE);

    if (!pBuffer1 || !pBuffer2)
    {
        if (pBuffer1) free(pBuffer1);
        if (pBuffer2) free(pBuffer2);

        MessageBeep(MB_ICONEXCLAMATION);
        MessageBox(hInstance, szMemError, szAppName,
            MB_ICONEXCLAMATION | MB_OK);
        return TRUE;
    }

    // Open waveform audio for input

    waveform.wFormatTag = WAVE_FORMAT_PCM;
    waveform.nChannels = 1;
    waveform.nSamplesPerSec = 11025;
    waveform.nAvgBytesPerSec = 11025;
    waveform.nBlockAlign = 1;
    waveform.wBitsPerSample = 8;
    waveform.cbSize = 0;

    if (waveInOpen(&hWaveIn, WAVE_MAPPER, &waveform,
        (DWORD)hInstance, 0, CALLBACK_WINDOW))
    {
        free(pBuffer1);
        free(pBuffer2);
        MessageBeep(MB_ICONEXCLAMATION);
        MessageBox(hInstance, szOpenError, szAppName,
            MB_ICONEXCLAMATION | MB_OK);
    }
    // Set up headers and prepare them

    pWaveHdr1->lpData = pBuffer1;
    pWaveHdr1->dwBufferLength = INP_BUFFER_SIZE;
    pWaveHdr1->dwBytesRecorded = 0;
    pWaveHdr1->dwUser = 0;
    pWaveHdr1->dwFlags = 0;
    pWaveHdr1->dwLoops = 1;
    pWaveHdr1->lpNext = NULL;
    pWaveHdr1->reserved = 0;

    waveInPrepareHeader(hWaveIn, pWaveHdr1, sizeof(WAVEHDR));

    pWaveHdr2->lpData = pBuffer2;
    pWaveHdr2->dwBufferLength = INP_BUFFER_SIZE;
    pWaveHdr2->dwBytesRecorded = 0;
    pWaveHdr2->dwUser = 0;
    pWaveHdr2->dwFlags = 0;
    pWaveHdr2->dwLoops = 1;
    pWaveHdr2->lpNext = NULL;
    pWaveHdr2->reserved = 0;

    waveInPrepareHeader(hWaveIn, pWaveHdr2, sizeof(WAVEHDR));
}

SOUND_PROCESSING_API void EndRecord() {
    // Reset input to return last buffer

    bEnding = TRUE;
    waveInReset(hWaveIn);
}

SOUND_PROCESSING_API BOOL WinProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam)
{


    switch (message)
    {
    case WM_COMMAND:
        switch (LOWORD(wParam))
        {
        case IDC_RECORD_BEG:
            return TRUE;

        case IDC_RECORD_END:
            return TRUE;

        case IDC_PLAY_BEG:
            // Open waveform audio for output

            waveform.wFormatTag = WAVE_FORMAT_PCM;
            waveform.nChannels = 1;
            waveform.nSamplesPerSec = 11025;
            waveform.nAvgBytesPerSec = 11025;
            waveform.nBlockAlign = 1;
            waveform.wBitsPerSample = 8;
            waveform.cbSize = 0;

            if (waveOutOpen(&hWaveOut, WAVE_MAPPER, &waveform,
                (DWORD)hwnd, 0, CALLBACK_WINDOW))
            {
                MessageBeep(MB_ICONEXCLAMATION);
                MessageBox(hwnd, szOpenError, szAppName,
                    MB_ICONEXCLAMATION | MB_OK);
            }
            return TRUE;

        case IDC_PLAY_PAUSE:
            // Pause or restart output

            if (!bPaused)
            {
                waveOutPause(hWaveOut);
                SetDlgItemText(hwnd, IDC_PLAY_PAUSE, TEXT("Resume"));
                bPaused = TRUE;
            }
            else
            {
                waveOutRestart(hWaveOut);
                SetDlgItemText(hwnd, IDC_PLAY_PAUSE, TEXT("Pause"));
                bPaused = FALSE;
            }
            return TRUE;

        case IDC_PLAY_END:
            // Reset output for close preparation

            bEnding = TRUE;
            waveOutReset(hWaveOut);
            return TRUE;

        case IDC_PLAY_REV:
            // Reverse save buffer and play

            bReverse = TRUE;
            ReverseMemory(pSaveBuffer, dwDataLength);

            SendMessage(hwnd, WM_COMMAND, IDC_PLAY_BEG, 0);
            return TRUE;

        case IDC_PLAY_REP:
            // Set infinite repetitions and play

            dwRepetitions = -1;
            SendMessage(hwnd, WM_COMMAND, IDC_PLAY_BEG, 0);
            return TRUE;

        case IDC_PLAY_SPEED:
            // Open waveform audio for fast output

            waveform.wFormatTag = WAVE_FORMAT_PCM;
            waveform.nChannels = 1;
            waveform.nSamplesPerSec = 22050;
            waveform.nAvgBytesPerSec = 22050;
            waveform.nBlockAlign = 1;
            waveform.wBitsPerSample = 8;
            waveform.cbSize = 0;

            if (waveOutOpen(&hWaveOut, 0, &waveform, (DWORD)hwnd, 0,
                CALLBACK_WINDOW))
            {
                MessageBeep(MB_ICONEXCLAMATION);
                MessageBox(hwnd, szOpenError, szAppName,
                    MB_ICONEXCLAMATION | MB_OK);
            }
            return TRUE;
        }
        break;

    case MM_WIM_OPEN:
        // Shrink down the save buffer

        pSaveBuffer = realloc(pSaveBuffer, 1);

        // Add the buffers

        waveInAddBuffer(hWaveIn, pWaveHdr1, sizeof(WAVEHDR));
        waveInAddBuffer(hWaveIn, pWaveHdr2, sizeof(WAVEHDR));

        // Begin sampling

        bRecording = TRUE;
        bEnding = FALSE;
        dwDataLength = 0;
        waveInStart(hWaveIn);
        return TRUE;

    case MM_WIM_DATA:

        // Reallocate save buffer memory

        pNewBuffer = realloc(pSaveBuffer, dwDataLength +
            ((PWAVEHDR)lParam)->dwBytesRecorded);

        if (pNewBuffer == NULL)
        {
            waveInClose(hWaveIn);
            MessageBeep(MB_ICONEXCLAMATION);
            MessageBox(hwnd, szMemError, szAppName,
                MB_ICONEXCLAMATION | MB_OK);
            return TRUE;
        }

        pSaveBuffer = pNewBuffer;
        CopyMemory(pSaveBuffer + dwDataLength, ((PWAVEHDR)lParam)->lpData,
            ((PWAVEHDR)lParam)->dwBytesRecorded);

        dwDataLength += ((PWAVEHDR)lParam)->dwBytesRecorded;

        if (bEnding)
        {
            waveInClose(hWaveIn);
            return TRUE;
        }

        // Send out a new buffer

        waveInAddBuffer(hWaveIn, (PWAVEHDR)lParam, sizeof(WAVEHDR));
        return TRUE;

    case MM_WIM_CLOSE:
        // Free the buffer memory

        waveInUnprepareHeader(hWaveIn, pWaveHdr1, sizeof(WAVEHDR));
        waveInUnprepareHeader(hWaveIn, pWaveHdr2, sizeof(WAVEHDR));

        free(pBuffer1);
        free(pBuffer2);
        bRecording = FALSE;

        if (bTerminating)
            SendMessage(hwnd, WM_SYSCOMMAND, SC_CLOSE, 0L);

        return TRUE;

    case MM_WOM_OPEN:

        // Set up header

        pWaveHdr1->lpData = pSaveBuffer;
        pWaveHdr1->dwBufferLength = dwDataLength;
        pWaveHdr1->dwBytesRecorded = 0;
        pWaveHdr1->dwUser = 0;
        pWaveHdr1->dwFlags = WHDR_BEGINLOOP | WHDR_ENDLOOP;
        pWaveHdr1->dwLoops = dwRepetitions;
        pWaveHdr1->lpNext = NULL;
        pWaveHdr1->reserved = 0;

        // Prepare and write

        waveOutPrepareHeader(hWaveOut, pWaveHdr1, sizeof(WAVEHDR));
        waveOutWrite(hWaveOut, pWaveHdr1, sizeof(WAVEHDR));

        bEnding = FALSE;
        bPlaying = TRUE;
        return TRUE;

    case MM_WOM_DONE:
        waveOutUnprepareHeader(hWaveOut, pWaveHdr1, sizeof(WAVEHDR));
        waveOutClose(hWaveOut);
        return TRUE;

    case MM_WOM_CLOSE:
        bPaused = FALSE;
        dwRepetitions = 1;
        bPlaying = FALSE;

        if (bReverse)
        {
            ReverseMemory(pSaveBuffer, dwDataLength);
            bReverse = FALSE;
        }

        if (bTerminating)
            SendMessage(hwnd, WM_SYSCOMMAND, SC_CLOSE, 0L);

        return TRUE;

    case WM_SYSCOMMAND:
        switch (LOWORD(wParam))
        {
        case SC_CLOSE:
            if (bRecording)
            {
                bTerminating = TRUE;
                bEnding = TRUE;
                waveInReset(hWaveIn);
                return TRUE;
            }

            if (bPlaying)
            {
                bTerminating = TRUE;
                bEnding = TRUE;
                waveOutReset(hWaveOut);
                return TRUE;
            }

            free(pWaveHdr1);
            free(pWaveHdr2);
            free(pSaveBuffer);
            EndDialog(hwnd, 0);
            return TRUE;
        }
        break;
    }
    return FALSE;
}

