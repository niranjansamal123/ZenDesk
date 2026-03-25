using System;
using System.Runtime.InteropServices;

namespace InvisiwindCS.Native
{
    public static class Win32
    {
        // ── Window enumeration ──────────────────────────────────────────
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] public static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);

        // ── DWM (skip cloaked windows) ──────────────────────────────────
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out uint pvAttribute, uint cbAttribute);
        public const uint DWMWA_CLOAKED = 14;

        // ── Hide / Show ─────────────────────────────────────────────────
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
        public const uint WDA_NONE = 0x00000000;
        public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        // ── Alt+Tab / Taskbar ───────────────────────────────────────────
        [DllImport("user32.dll", SetLastError = true)] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", SetLastError = true)] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_APPWINDOW = 0x00040000;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_FRAMECHANGED = 0x0020;
        // Add inside Win32 class:
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_VM_READ = 0x0010;


        // ── Icons ───────────────────────────────────────────────────────
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] public static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] public static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);          // ✅ user32
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC); // ✅ user32
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")] public static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint cLines, byte[] lpvBits, ref BITMAPINFO lpbmi, uint usage);
        [DllImport("gdi32.dll")] public static extern int GetObject(IntPtr hObject, int nCount, ref BITMAP lpObject);

        public const uint WM_GETICON = 0x007F;
        public const int ICON_SMALL2 = 2;
        public const int GCLP_HICONSM = -34;
        public const uint DIB_RGB_COLORS = 0;
        public const uint BI_RGB = 0;
        // Icon message constants
        public const int ICON_SMALL = 0;       // ← ADD THIS
        public const int ICON_BIG = 1;       // ← ADD THIS
        
        public const int GCLP_HICON = -14;    // ← ADD THIS


        // ── Injection ───────────────────────────────────────────────────
        [DllImport("kernel32.dll")] public static extern IntPtr OpenProcess(uint dwAccess, bool bInherit, uint pid);
        [DllImport("kernel32.dll")] public static extern IntPtr VirtualAllocEx(IntPtr hProc, IntPtr addr, uint size, uint allocType, uint protect);
        [DllImport("kernel32.dll")] public static extern bool WriteProcessMemory(IntPtr hProc, IntPtr baseAddr, byte[] buf, uint size, out IntPtr written);
        [DllImport("kernel32.dll")] public static extern IntPtr GetProcAddress(IntPtr hMod, string name);
        [DllImport("kernel32.dll")] public static extern IntPtr GetModuleHandle(string name);
        [DllImport("kernel32.dll")] public static extern IntPtr CreateRemoteThread(IntPtr hProc, IntPtr attr, uint stackSize, IntPtr startAddr, IntPtr param, uint flags, IntPtr tid);
        [DllImport("kernel32.dll")] public static extern uint WaitForSingleObject(IntPtr hObj, uint ms);
        [DllImport("kernel32.dll")] public static extern bool VirtualFreeEx(IntPtr hProc, IntPtr addr, uint size, uint freeType);
        [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll")] public static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
        [DllImport("kernel32.dll")] public static extern bool FreeLibrary(IntPtr hMod);
        [DllImport("kernel32.dll")] public static extern bool IsWow64Process(IntPtr hProc, out bool wow64);

        public const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        public const uint MEM_COMMIT_RESERVE = 0x3000;
        public const uint MEM_RELEASE = 0x8000;
        public const uint PAGE_READWRITE = 0x04;

        // ── Structs ─────────────────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential)]
        public struct ICONINFO
        {
            public bool fIcon;
            public uint xHotspot;
            public uint yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public IntPtr bmBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public uint[] bmiColors;
        }
    }
}
