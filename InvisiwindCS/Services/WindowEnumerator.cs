using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using InvisiwindCS.Models;
using InvisiwindCS.Native;

namespace InvisiwindCS.Services
{
    public static class WindowEnumerator
    {
        public static List<WindowInfo> GetTopLevelWindows()
        {
            var result = new List<WindowInfo>();
            try
            {
                Win32.EnumWindows((hWnd, _) =>
                {
                    try
                    {
                        if (!Win32.IsWindowVisible(hWnd)) return true;

                        var sb = new StringBuilder(256);
                        if (Win32.GetWindowText(hWnd, sb, 256) == 0) return true;
                        string title = sb.ToString().Trim();
                        if (string.IsNullOrEmpty(title)) return true;

                        // Skip cloaked windows
                        try
                        {
                            Win32.DwmGetWindowAttribute(hWnd, Win32.DWMWA_CLOAKED,
                                out uint cloaked, sizeof(uint));
                            if (cloaked != 0) return true;
                        }
                        catch { }

                        Win32.GetWindowDisplayAffinity(hWnd, out uint affinity);
                        bool isHidden = affinity != 0;

                        Win32.GetWindowThreadProcessId(hWnd, out uint pid);
                        if (pid == 0) return true;

                        bool isX86 = false;
                        try
                        {
                            IntPtr hProc = Win32.OpenProcess(0x1000, false, pid);
                            if (hProc != IntPtr.Zero)
                            {
                                Win32.IsWow64Process(hProc, out isX86);
                                Win32.CloseHandle(hProc);
                            }
                        }
                        catch { }

                        result.Add(new WindowInfo
                        {
                            Handle = hWnd,
                            Title = title,
                            Pid = pid,
                            IsHidden = isHidden,
                            IsX86 = isX86
                        });
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                LoggerService.Error($"EnumWindows failed: {ex.Message}");
            }
            return result;
        }
    }
}
