using CommunityToolkit.Mvvm.Messaging;
using D4Companion.SystemPresets.Messages;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vortice;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace D4Companion.SystemPresets
{
    public sealed class MonitorDuplicator : IDisposable
    {
        private readonly IDXGIAdapter1 _adapter;
        private readonly uint _adapterIndex;
        private readonly IDXGIOutput1 _output;
        private readonly uint _outputIndex;
        private readonly ID3D11Device? _device;
        private readonly ID3D11DeviceContext? _context;
        private readonly IDXGIOutputDuplication _duplication;

        private int _previousCursorX = 0;
        private int _previousCursorY = 0;

        // Start of Constructors region

        #region Constructors

        public MonitorDuplicator(IDXGIFactory1 factory, uint adapterIndex, uint outputIndex)
        {
            _adapterIndex = adapterIndex;
            _outputIndex = outputIndex;

            factory.EnumAdapters1(adapterIndex, out _adapter).CheckError();

            FeatureLevel[] levels =
            {
                FeatureLevel.Level_12_2,
                FeatureLevel.Level_12_1,
                FeatureLevel.Level_12_0,
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0
            };

            D3D11.D3D11CreateDevice(_adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport, levels, out _device).CheckError();

            _context = _device?.ImmediateContext;
            _adapter.EnumOutputs(outputIndex, out IDXGIOutput output).CheckError();
            _adapter.Dispose();

            var desc = output.Description;
            if (!desc.AttachedToDesktop)
                throw new InvalidOperationException("Output is not a desktop-attached monitor.");

            _output = output.QueryInterface<IDXGIOutput1>();
            _duplication = _output.DuplicateOutput(_device);
        }

        #endregion

        // Start of Properties region

        #region Properties

        public ID3D11Device? Device => _device;

        public string DeviceName => _output.Description.DeviceName;

        #endregion

        // Start of Methods region

        #region Methods

        public string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }

        public string ReplaceInvalidChars(string filename, string replacement)
        {
            return string.Join(replacement, filename.Split(Path.GetInvalidFileNameChars()));
        }

        public override string ToString()
        {
            string deviceName = _output.Description.DeviceName;
            deviceName = RemoveInvalidChars(deviceName);

            return deviceName;
        }

        public void Dispose()
        {
            _duplication?.Dispose();
            _output?.Dispose();
            _context?.Dispose();
            _device?.Dispose();
            _adapter?.Dispose();
        }        

        public (BitmapSource?, int x, int y) TryGetScreen()
        {
            var result = _duplication.AcquireNextFrame(0, out var frameInfo, out var dskTopResource);
            if (result.Failure)
            {
                dskTopResource?.Dispose();
                _duplication.ReleaseFrame();
                return (null, 0, 0);
            }

            var pointerInfo = frameInfo.PointerPosition;
            bool cursorVisible = pointerInfo.Visible;
            int cursorX = _previousCursorX;
            int cursorY = _previousCursorY;

            if (pointerInfo.Position.X != 0 || pointerInfo.Position.Y != 0)
            {
                cursorX = pointerInfo.Position.X;
                cursorY = pointerInfo.Position.Y;
            }

            _previousCursorX = cursorX;
            _previousCursorY = cursorY;

            var desktopLeft = _output.Description.DesktopCoordinates.Left;
            var desktopTop = _output.Description.DesktopCoordinates.Top;

            //Debug.WriteLine($"{_output.Description.DeviceName}");
            //Debug.WriteLine($"Cursor Position: ({cursorX}, {cursorY}), Visible: {cursorVisible}");
            //Debug.WriteLine($"Desktop Coordinates: Left={desktopLeft}, Top={desktopTop}");
            //Debug.WriteLine("");

            using var frameTexture = dskTopResource.QueryInterface<ID3D11Texture2D>();
            var textureDesc = new Texture2DDescription
            {
                CPUAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = frameTexture.Description.Width,
                Height = frameTexture.Description.Height,
                MiscFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            using var currentFrame = _device!.CreateTexture2D(textureDesc);
            using var desktopResource = dskTopResource;

            _device.ImmediateContext.CopyResource(currentFrame, frameTexture);
            var dataBox = _device.ImmediateContext.Map(currentFrame, 0);

            int stride = (int)frameTexture.Description.Width * 4; // BGRA32
            byte[] pixels = new byte[stride * frameTexture.Description.Height];

            unsafe
            {
                fixed (byte* pDest = pixels)
                {
                    byte* pSrc = (byte*)dataBox.DataPointer;

                    for (int y = 0; y < frameTexture.Description.Height; y++)
                    {
                        Buffer.MemoryCopy(pSrc + y * dataBox.RowPitch, pDest + y * stride, stride, stride);
                    }
                }
            }

            _duplication.ReleaseFrame();
            _device.ImmediateContext.Unmap(currentFrame, 0);

            var bitmapSource = BitmapSource.Create((int)frameTexture.Description.Width, (int)frameTexture.Description.Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
            var bitmapSourceWithCursor = DrawCursor(bitmapSource, cursorX, cursorY);

            return (bitmapSourceWithCursor, cursorX, cursorY);
        }

        private BitmapSource DrawCursor(BitmapSource bitmapSource, int cursorX, int cursorY)
        {
            var renderTargetBitmap = new RenderTargetBitmap(
                bitmapSource.PixelWidth,
                bitmapSource.PixelHeight,
                bitmapSource.DpiX,
                bitmapSource.DpiY,
                PixelFormats.Pbgra32);

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Draw original image
                drawingContext.DrawImage(bitmapSource, new Rect(0, 0, bitmapSource.PixelWidth, bitmapSource.PixelHeight));

                // Draw cursor
                if (cursorX != 0 || cursorY != 0)
                {
                    double pixelsPerDip = bitmapSource.DpiX / 96.0;
                    var ft = new FormattedText(
                        "X",
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        20,
                        Brushes.Red,
                        pixelsPerDip);

                    drawingContext.DrawText(ft, new Point(cursorX, cursorY));
                }                
            }

            renderTargetBitmap.Render(drawingVisual);
            return renderTargetBitmap;
        }        

        #endregion
    }
}