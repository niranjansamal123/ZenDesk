#include "pch.h"
#include <Windows.h>

#define WDA_EXCLUDEFROMCAPTURE 0x00000011
#define WDA_NONE               0x00000000

// Packed parameter struct passed via remote memory
struct HideParams {
    HWND hwnd;
    BOOL hide;
};

extern "C" __declspec(dllexport)
DWORD WINAPI SetWindowVisibility(LPVOID lpParam)
{
    HideParams* p = (HideParams*)lpParam;
    DWORD affinity = p->hide ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE;
    SetWindowDisplayAffinity(p->hwnd, affinity);
    return 0;
}

extern "C" __declspec(dllexport)
DWORD WINAPI HideFromTaskbar(LPVOID lpParam)
{
    HideParams* p = (HideParams*)lpParam;
    LONG style = GetWindowLongW(p->hwnd, GWL_EXSTYLE);
    if (style == 0) return 1;

    if (p->hide) {
        style |= WS_EX_TOOLWINDOW;
        style &= ~WS_EX_APPWINDOW;
    }
    else {
        style |= WS_EX_APPWINDOW;
        style &= ~WS_EX_TOOLWINDOW;
    }
    SetWindowLongW(p->hwnd, GWL_EXSTYLE, style);
    return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    return TRUE;
}
