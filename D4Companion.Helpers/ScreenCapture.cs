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
                windowDCHandle, region.left, region.top, ROP_CODE.SRCCOPY);

            // ROP_CODE.SRCCOPY: Set BitBlt to copy pixels normally  
            // ROP_CODE.CAPTUREBLT: Tells BitBlt to include layered/transparent windows in the capture. Causes mouse flickering.

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

        public List<(string, BitmapSource?)> GetAllMonitorCapture()
        {
            var results = new List<(string, BitmapSource?)>();

            unsafe
            {
                PInvoke.EnumDisplayMonitors(
                    hdc: default,
                    lprcClip: null,
                    lpfnEnum: (HMONITOR monitor, HDC hdcMonitor, RECT* rectPtr, LPARAM data) =>
                    {
                        // Device name                       
                        MONITORINFOEXW info = new MONITORINFOEXW();
                        info.monitorInfo.cbSize = (uint)sizeof(MONITORINFOEXW);

                        string deviceName = string.Empty;
                        if (PInvoke.GetMonitorInfo(monitor, (MONITORINFO*)&info))
                        {
                            deviceName = info.szDevice.ToString();
                        }

                        // Bitmap
                        RECT rect = *rectPtr;
                        results.Add((deviceName, ImageSourceFromBitmap(CaptureMonitor(rect))));
                        return true;
                    },
                    dwData: 0
                );
            }

            return results;
        }

        private Bitmap CaptureMonitor(RECT rect)
        {
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            var desktop = PInvoke.GetDesktopWindow();
            var desktopDC = PInvoke.GetWindowDC(desktop);
            var memDC = PInvoke.CreateCompatibleDC(desktopDC);

            var bmpHandle = PInvoke.CreateCompatibleBitmap(desktopDC, width, height);
            var oldObj = PInvoke.SelectObject(memDC, bmpHandle);

            PInvoke.BitBlt(
                memDC,
                0, 0,
                width, height,
                desktopDC,
                rect.left, rect.top,
                ROP_CODE.SRCCOPY
            );

            Bitmap bmp;

            try
            {
                bmp = Image.FromHbitmap(bmpHandle);
            }
            finally
            {
                PInvoke.SelectObject(memDC, oldObj);
                PInvoke.DeleteObject(bmpHandle);
                PInvoke.DeleteDC(memDC);
                PInvoke.ReleaseDC(desktop, desktopDC);
            }

            return bmp;
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

        public static Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            Bitmap bmp = new Bitmap(
              bitmapsource.PixelWidth,
              bitmapsource.PixelHeight,
              PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
              new Rectangle(System.Drawing.Point.Empty, bmp.Size),
              ImageLockMode.WriteOnly,
              PixelFormat.Format32bppPArgb);
            bitmapsource.CopyPixels(
              Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        //public static Bitmap BitmapFromSource(BitmapSource bitmapsource)
        //{
        //    // Note: This conversion sometimes misses the last (bottom) part of images.

        //    Bitmap bitmap;
        //    using (var outStream = new MemoryStream())
        //    {
        //        BitmapEncoder enc = new BmpBitmapEncoder();
        //        enc.Frames.Add(BitmapFrame.Create(bitmapsource));
        //        enc.Save(outStream);
        //        bitmap = new Bitmap(outStream);
        //    }
        //    return bitmap;
        //}

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
