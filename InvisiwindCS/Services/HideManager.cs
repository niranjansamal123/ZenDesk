using InvisiwindCS.Native;
using System;

namespace InvisiwindCS.Services
{
    public static class HideManager
    {
        // Direct call — only works on windows in our own process
        public static bool SetWindowVisibility(IntPtr hwnd, bool hide)
        {
            uint affinity = hide ? Win32.WDA_EXCLUDEFROMCAPTURE : Win32.WDA_NONE;
            return Win32.SetWindowDisplayAffinity(hwnd, affinity);
        }

        // Direct call — Alt+Tab / Taskbar hiding
        public static bool HideFromTaskbar(IntPtr hwnd, bool hide)
        {
            int style = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
            if (style == 0) return false;

            if (hide)
            {
                style |= Win32.WS_EX_TOOLWINDOW;
                style &= ~Win32.WS_EX_APPWINDOW;
            }
            else
            {
                style |= Win32.WS_EX_APPWINDOW;
                style &= ~Win32.WS_EX_TOOLWINDOW;
            }

            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, style);
            Win32.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_FRAMECHANGED);
            return true;
        }
    }
}
