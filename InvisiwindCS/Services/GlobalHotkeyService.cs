using System;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using InvisiwindCS.Services;

namespace InvisiwindCS.Services
{
    public class GlobalHotkeyService : IDisposable
    {
        private readonly Action<string> _onLog;

        // Ctrl+J = hide/unhide foreground window (same as hide.ahk)
        public GlobalHotkeyService(Action<string> onLog)
        {
            _onLog = onLog;
            Register();
        }

        void Register()
        {
            try
            {
                HotkeyManager.Current.AddOrReplace(
                    "ToggleHide",
                    Key.J,
                    ModifierKeys.Control,
                    OnToggleHide);

                HotkeyManager.Current.AddOrReplace(
                    "HideWindow",
                    Key.H,
                    ModifierKeys.Control | ModifierKeys.Shift,
                    OnHideWindow);

                HotkeyManager.Current.AddOrReplace(
                    "UnhideAll",
                    Key.U,
                    ModifierKeys.Control | ModifierKeys.Shift,
                    OnUnhideAll);

                _onLog("Hotkeys registered: Ctrl+J (toggle), Ctrl+Shift+H (hide), Ctrl+Shift+U (unhide all)");
            }
            catch (Exception ex)
            {
                _onLog($"Hotkey registration failed: {ex.Message}");
            }
        }

        // Ctrl+J — toggle hide/unhide on the currently focused window
        void OnToggleHide(object? sender, HotkeyEventArgs e)
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

            Native.Win32.GetWindowDisplayAffinity(hwnd, out uint affinity);
            bool isHidden = affinity != 0;

            Native.Win32.GetWindowThreadProcessId(hwnd, out uint pid);

            // Get x86 status
            bool isX86 = false;
            IntPtr hProc = Native.Win32.OpenProcess(0x1000, false, pid);
            if (hProc != IntPtr.Zero)
            {
                Native.Win32.IsWow64Process(hProc, out isX86);
                Native.Win32.CloseHandle(hProc);
            }

            bool newState = !isHidden;
            WindowInjector.SetWindowProps(pid, hwnd, newState, null, isX86);
            _onLog($"Ctrl+J: {(newState ? "Hidden" : "Shown")} window 0x{hwnd:X}");

            e.Handled = true;
        }

        // Ctrl+Shift+H — hide focused window
        void OnHideWindow(object? sender, HotkeyEventArgs e)
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

            Native.Win32.GetWindowThreadProcessId(hwnd, out uint pid);
            bool isX86 = false;
            IntPtr hProc = Native.Win32.OpenProcess(0x1000, false, pid);
            if (hProc != IntPtr.Zero) { Native.Win32.IsWow64Process(hProc, out isX86); Native.Win32.CloseHandle(hProc); }

            WindowInjector.SetWindowProps(pid, hwnd, true, null, isX86);
            _onLog($"Ctrl+Shift+H: Hidden 0x{hwnd:X}");
            e.Handled = true;
        }

        // Ctrl+Shift+U — unhide ALL currently hidden windows
        void OnUnhideAll(object? sender, HotkeyEventArgs e)
        {
            var windows = WindowEnumerator.GetTopLevelWindows();
            int count = 0;
            foreach (var w in windows)
            {
                if (!w.IsHidden) continue;
                WindowInjector.SetWindowProps(w.Pid, w.Handle, false, false, w.IsX86);
                count++;
            }
            _onLog($"Ctrl+Shift+U: Unhid {count} windows");
            e.Handled = true;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        public void Dispose()
        {
            HotkeyManager.Current.Remove("ToggleHide");
            HotkeyManager.Current.Remove("HideWindow");
            HotkeyManager.Current.Remove("UnhideAll");
        }
    }
}
