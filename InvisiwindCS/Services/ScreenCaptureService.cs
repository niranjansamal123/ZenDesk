using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media.Imaging;

namespace InvisiwindCS.Services
{
    public class ScreenCaptureService : IDisposable
    {
        private Timer? _timer;
        private bool _running;
        private bool _disposed;

        public event Action<BitmapSource>? FrameArrived;

        // ── Win32 cursor APIs ───────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(ref CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("user32.dll")]
        static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop,
            IntPtr hIcon, int cxWidth, int cyHeight,
            int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        private const int CURSOR_SHOWING = 0x00000001;
        private const int DI_NORMAL = 0x0003;

        // ── Public API ──────────────────────────────────────────────────
        public void Start()
        {
            if (_running || _disposed) return;
            _running = true;
            _timer = new Timer(_ => CaptureFrame(), null, 0, 33); // ~30fps
        }

        public void Stop()
        {
            _running = false;
            _timer?.Dispose();
            _timer = null;
        }

        // ── Capture Frame ───────────────────────────────────────────────
        void CaptureFrame()
        {
            if (!_running || _disposed) return;
            try
            {
                var screen = System.Windows.Forms.Screen.PrimaryScreen;
                if (screen == null) return;

                var bounds = screen.Bounds;
                if (bounds.Width <= 0 || bounds.Height <= 0) return;

                using var bmp = new Bitmap(bounds.Width, bounds.Height,
                    PixelFormat.Format32bppArgb);

                using (var g = Graphics.FromImage(bmp))
                {
                    // Step 1: Capture screen
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                    // Step 2: Draw cursor on top
                    DrawCursor(g);
                }

                var src = ToBitmapSource(bmp);
                src.Freeze();
                FrameArrived?.Invoke(src);
            }
            catch { /* Never crash on capture failure */ }
        }

        // ── Draw Cursor ─────────────────────────────────────────────────
        void DrawCursor(Graphics g)
        {
            try
            {
                var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
                if (!GetCursorInfo(ref ci)) return;
                if ((ci.flags & CURSOR_SHOWING) == 0) return;

                // Get hotspot offset
                int hotX = 0, hotY = 0;
                if (GetIconInfo(ci.hCursor, out ICONINFO ii))
                {
                    hotX = ii.xHotspot;
                    hotY = ii.yHotspot;
                    if (ii.hbmColor != IntPtr.Zero) DeleteObject(ii.hbmColor);
                    if (ii.hbmMask != IntPtr.Zero) DeleteObject(ii.hbmMask);
                }

                // Draw cursor at correct position accounting for hotspot
                IntPtr hdc = g.GetHdc();
                try
                {
                    DrawIconEx(hdc,
                        ci.ptScreenPos.x - hotX,
                        ci.ptScreenPos.y - hotY,
                        ci.hCursor,
                        0, 0, 0, IntPtr.Zero, DI_NORMAL);
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }
            catch { }
        }

        // ── Convert Bitmap to BitmapSource ──────────────────────────────
        static BitmapSource ToBitmapSource(Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            var img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = ms;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            return img;
        }

        // ── Dispose ─────────────────────────────────────────────────────
        public void Dispose()
        {
            _disposed = true;
            Stop();
        }
    }
}
