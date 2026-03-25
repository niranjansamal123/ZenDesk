using System;
using System.Runtime.InteropServices;
using InvisiwindCS.Native;

namespace InvisiwindCS.Services
{
    public static class PeekThroughService
    {
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const uint LWA_ALPHA = 0x00000002;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetLayeredWindowAttributes(
            IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool GetLayeredWindowAttributes(
            IntPtr hwnd, out uint crKey, out byte bAlpha, out uint dwFlags);

        // ── Peek state ──────────────────────────────────────────────────
        private static IntPtr _peekHwnd = IntPtr.Zero;
        private static uint _origAffinity = 0;
        private static int _origExStyle = 0;
        private static bool _isPeeking = false;

        public static bool IsPeeking => _isPeeking;
        public static IntPtr PeekHwnd => _peekHwnd;

        // ── Start Peek ──────────────────────────────────────────────────
        public static void StartPeek(IntPtr hwnd, byte opacity = 128)
        {
            if (_isPeeking) StopPeek();
            try
            {
                _peekHwnd = hwnd;
                Win32.GetWindowDisplayAffinity(hwnd, out _origAffinity);
                _origExStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);

                Win32.SetWindowDisplayAffinity(hwnd, Win32.WDA_NONE);

                int newStyle = _origExStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT;
                Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, newStyle);
                SetLayeredWindowAttributes(hwnd, 0, opacity, LWA_ALPHA);

                _isPeeking = true;
                LoggerService.Info($"Peek started: hwnd=0x{hwnd:X} opacity={opacity}");
            }
            catch (Exception ex)
            {
                LoggerService.Error($"StartPeek failed: {ex.Message}");
            }
        }

        // ── Update Opacity (peek slider) ────────────────────────────────
        public static void UpdateOpacity(byte opacity)
        {
            if (!_isPeeking || _peekHwnd == IntPtr.Zero) return;
            try { SetLayeredWindowAttributes(_peekHwnd, 0, opacity, LWA_ALPHA); }
            catch { }
        }

        // ── Update Opacity by HWND (global transparency feature) ────────
        public static void UpdateOpacity(IntPtr hwnd, byte opacity, bool clickThrough = true)
        {
            try
            {
                int exStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);

                exStyle |= WS_EX_LAYERED;

                if (clickThrough)
                    exStyle |= WS_EX_TRANSPARENT;
                else
                    exStyle &= ~WS_EX_TRANSPARENT;

                Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, exStyle);
                SetLayeredWindowAttributes(hwnd, 0, opacity, LWA_ALPHA);
            }
            catch { }
        }

        // ── Set Click-Through only (no transparency) ────────────────────────
        public static void SetClickThrough(IntPtr hwnd, bool enable)
        {
            try
            {
                int exStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);

                if (enable)
                {
                    // Add both flags — WS_EX_TRANSPARENT requires WS_EX_LAYERED
                    exStyle |= WS_EX_LAYERED;
                    exStyle |= WS_EX_TRANSPARENT;
                    Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, exStyle);

                    // Keep existing opacity if already layered, else set full opacity
                    bool alreadyLayered = (exStyle & WS_EX_LAYERED) != 0;
                    if (!alreadyLayered)
                        SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);
                }
                else
                {
                    // ✅ Only remove TRANSPARENT — keep LAYERED if transparency is active
                    exStyle &= ~WS_EX_TRANSPARENT;

                    // Check if window still has opacity set (transparency feature active)
                    bool hasOpacity = GetLayeredWindowAttributes(
                        hwnd, out _, out byte alpha, out uint flags)
                        && alpha < 255;

                    if (!hasOpacity)
                    {
                        // Safe to remove LAYERED — no transparency active
                        exStyle &= ~WS_EX_LAYERED;
                    }

                    Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, exStyle);
                }

                LoggerService.Info($"SetClickThrough={enable} hwnd=0x{hwnd:X}");
            }
            catch (Exception ex)
            {
                LoggerService.Error($"SetClickThrough: {ex.Message}");
            }
        }


        // ── Restore Full Opacity + Remove Click-Through ──────────────────────
        public static void RestoreOpacity(IntPtr hwnd)
        {
            try
            {
                int exStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);

                bool isClickThrough = (exStyle & WS_EX_TRANSPARENT) != 0;

                if (isClickThrough)
                {
                    // Keep LAYERED + TRANSPARENT — just restore full opacity
                    SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);
                }
                else
                {
                    // Safe to remove both flags
                    exStyle &= ~WS_EX_LAYERED;
                    exStyle &= ~WS_EX_TRANSPARENT;
                    Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, exStyle);
                }
            }
            catch { }
        }


        // ── Stop Peek ───────────────────────────────────────────────────
        public static void StopPeek()
        {
            if (!_isPeeking || _peekHwnd == IntPtr.Zero) return;
            try
            {
                Win32.SetWindowDisplayAffinity(_peekHwnd, _origAffinity);
                Win32.SetWindowLong(_peekHwnd, Win32.GWL_EXSTYLE, _origExStyle);

                _isPeeking = false;
                _peekHwnd = IntPtr.Zero;
                LoggerService.Info("Peek stopped");
            }
            catch (Exception ex)
            {
                LoggerService.Error($"StopPeek failed: {ex.Message}");
            }
        }
    }
}
