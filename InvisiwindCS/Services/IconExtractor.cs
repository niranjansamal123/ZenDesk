using System;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using InvisiwindCS.Native;

namespace InvisiwindCS.Services
{
    public static class IconExtractor
    {
        public static ImageSource? GetWindowIcon(IntPtr hwnd)
        {
            try
            {
                IntPtr hIcon = Win32.SendMessage(hwnd, Win32.WM_GETICON, (IntPtr)Win32.ICON_SMALL2, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = Win32.SendMessage(hwnd, Win32.WM_GETICON, (IntPtr)Win32.ICON_SMALL, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = Win32.SendMessage(hwnd, Win32.WM_GETICON, (IntPtr)Win32.ICON_BIG, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = Win32.GetClassLongPtr(hwnd, Win32.GCLP_HICONSM);
                if (hIcon == IntPtr.Zero)
                    hIcon = Win32.GetClassLongPtr(hwnd, Win32.GCLP_HICON);
                if (hIcon == IntPtr.Zero) return null;

                if (!Win32.GetIconInfo(hIcon, out Win32.ICONINFO iconInfo)) return null;
                if (iconInfo.hbmColor == IntPtr.Zero) return null;

                IntPtr hdc = Win32.GetDC(IntPtr.Zero);
                if (hdc == IntPtr.Zero) return null;

                try
                {
                    Win32.BITMAP bmpObj = new();
                    if (Win32.GetObject(iconInfo.hbmColor,
                        Marshal.SizeOf<Win32.BITMAP>(), ref bmpObj) == 0) return null;

                    int w = bmpObj.bmWidth, h = bmpObj.bmHeight;
                    if (w <= 0 || h <= 0) return null;

                    var bmi = new Win32.BITMAPINFO
                    {
                        bmiHeader = new Win32.BITMAPINFOHEADER
                        {
                            biSize = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>(),
                            biWidth = w,
                            biHeight = -h,        // negative = top-down
                            biPlanes = 1,
                            biBitCount = 32,
                            biCompression = Win32.BI_RGB
                        },
                        bmiColors = new uint[1]
                    };

                    byte[] pixels = new byte[w * h * 4];
                    if (Win32.GetDIBits(hdc, iconInfo.hbmColor, 0, (uint)h,
                        pixels, ref bmi, Win32.DIB_RGB_COLORS) == 0) return null;

                    // ✅ Fix zero-alpha issue common in Windows DIB
                    bool hasAlpha = false;
                    for (int i = 3; i < pixels.Length; i += 4)
                        if (pixels[i] != 0) { hasAlpha = true; break; }
                    if (!hasAlpha)
                        for (int i = 3; i < pixels.Length; i += 4)
                            pixels[i] = 255;

                    // ✅ No BGR swap needed — Bgra32 matches Windows DIB format
                    var bmpSource = BitmapSource.Create(w, h, 96, 96,
                        PixelFormats.Bgra32, null, pixels, w * 4);
                    bmpSource.Freeze();
                    return bmpSource;
                }
                finally
                {
                    Win32.ReleaseDC(IntPtr.Zero, hdc);
                    if (iconInfo.hbmColor != IntPtr.Zero) Win32.DeleteObject(iconInfo.hbmColor);
                    if (iconInfo.hbmMask != IntPtr.Zero) Win32.DeleteObject(iconInfo.hbmMask);
                }
            }
            catch { return null; }
        }

    }
}
