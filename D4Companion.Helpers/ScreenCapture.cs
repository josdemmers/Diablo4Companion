using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace D4Companion.Helpers
{
    public class ScreenCapture
    {
        // Docs: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-bitblt
        const int SRCCOPY = 0x00CC0020;
        const int CAPTUREBLT = 0x40000000;

        public Bitmap GetScreenCapture(HWND windowHandle)
        {
            Bitmap bitmap;

            RECT region;
            PInvoke.GetWindowRect(windowHandle, out region);

            var desktopWindowHandle = PInvoke.GetDesktopWindow();
            var windowDCHandle = PInvoke.GetWindowDC(desktopWindowHandle);
            var memoryDCHandle = PInvoke.CreateCompatibleDC(windowDCHandle);
            var bitmapHandle = PInvoke.CreateCompatibleBitmap(windowDCHandle,
                region.right - region.left, region.bottom - region.top);
            var bitmapOldHandle = PInvoke.SelectObject(memoryDCHandle, bitmapHandle);

            bool status = PInvoke.BitBlt(memoryDCHandle, 0, 0,
                region.right - region.left, region.bottom - region.top,
                windowDCHandle, region.left, region.top, ROP_CODE.SRCCOPY | ROP_CODE.CAPTUREBLT);

            try
            {
                bitmap = Image.FromHbitmap(bitmapHandle);
            }
            finally
            {
                PInvoke.SelectObject(memoryDCHandle, bitmapOldHandle);
                PInvoke.DeleteObject(bitmapHandle);
                PInvoke.DeleteDC(memoryDCHandle);
                PInvoke.ReleaseDC(desktopWindowHandle, windowDCHandle);
            }

            return bitmap;
        }

        public Bitmap? GetScreenCaptureArea(HWND windowHandle, float roiLeft, float roiTop, float roiWidth, float roiHeight)
        {
            Bitmap? bitmap = null;

            RECT region;
            PInvoke.GetWindowRect(windowHandle, out region);

            int width = (int)roiWidth;
            int height = (int)roiHeight;

            int xPos = (int)roiLeft;
            int yPos = (int)roiTop;

            var desktopWindowHandle = PInvoke.GetDesktopWindow();
            var windowDCHandle = PInvoke.GetWindowDC(desktopWindowHandle);
            var memoryDCHandle = PInvoke.CreateCompatibleDC(windowDCHandle);
            var bitmapHandle = PInvoke.CreateCompatibleBitmap(windowDCHandle, width, height);
            var bitmapOldHandle = PInvoke.SelectObject(memoryDCHandle, bitmapHandle);

            bool status = PInvoke.BitBlt(memoryDCHandle, 0, 0, width, height, windowDCHandle, xPos, yPos, ROP_CODE.SRCCOPY | ROP_CODE.CAPTUREBLT);

            try
            {
                if (status)
                {
                    bitmap = Image.FromHbitmap(bitmapHandle);
                }
            }
            finally
            {
                PInvoke.SelectObject(memoryDCHandle, bitmapOldHandle);
                PInvoke.DeleteObject(bitmapHandle);
                PInvoke.DeleteDC(memoryDCHandle);
                PInvoke.ReleaseDC(desktopWindowHandle, windowDCHandle);
            }

            return bitmap;
        }

        public static BitmapSource? ImageSourceFromBitmap(Bitmap? bitmap)
        {
            if (bitmap != null)
            {
                var handle = bitmap.GetHbitmap();
                try
                {
                    return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    PInvoke.DeleteObject((HGDIOBJ)handle);
                }
            }
            else
            {
                return null;
            }
        }

        public static void WriteBitmapToFile(string filename, Bitmap? bitmap)
        {
            // Check folder
            string folder = new FileInfo(filename)?.Directory?.FullName ?? string.Empty;
            if (!Directory.Exists(folder) && !string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);

            // Use the OpenCv save function instead
            // https://stackoverflow.com/questions/52100703/bug-in-windows-nets-system-drawing-savestream-imageformat-corrupt-png-pro
            bitmap?.Save(filename, ImageFormat.Png);
        }
    }
}
