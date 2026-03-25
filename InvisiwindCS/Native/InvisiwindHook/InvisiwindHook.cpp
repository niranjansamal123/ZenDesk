#include <Windows.h>

#define WDA_EXCLUDEFROMCAPTURE 0x00000011
#define WDA_NONE               0x00000000

extern "C" __declspec(dllexport)
bool __stdcall SetWindowVisibility(HWND hwnd, bool hide) {
    DWORD affinity = hide ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE;
    return SetWindowDisplayAffinity(hwnd, affinity);
}

extern "C" __declspec(dllexport)
bool __stdcall HideFromTaskbar(HWND hwnd, bool hide) {
    LONG style = GetWindowLongW(hwnd, GWL_EXSTYLE);
    if (style == 0) return false;

    if (hide) {
        style |=  WS_EX_TOOLWINDOW;
        style &= ~WS_EX_APPWINDOW;
    } else {
        style |=  WS_EX_APPWINDOW;
        style &= ~WS_EX_TOOLWINDOW;
    }

    SetWindowLongW(hwnd, GWL_EXSTYLE, style);
    return true;
}

BOOL WINAPI DllMain(HINSTANCE, DWORD, LPVOID) { return TRUE; }
