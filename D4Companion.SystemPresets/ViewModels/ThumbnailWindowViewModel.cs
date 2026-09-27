using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.SystemPresets.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace D4Companion.SystemPresets.ViewModels
{
    public class ThumbnailWindowViewModel : ObservableObject
    {
        private HWND _handle = HWND.Null;
        private HWND _handleSource = HWND.Null;
        private RECT _regionSource = new RECT();
        private IntPtr _thumbnailHandle = IntPtr.Zero;

        private double _height = 720;
        private double _left = 0;
        private double _top = 0;
        private double _width = 1280;

        private int _offsetTop = 0;
        private int _offsetLeft = 0;

        private double _delayUpdateMouse = 25;
        private D4Companion.Helpers.ScreenCapture _screenCaptureGDI = new D4Companion.Helpers.ScreenCapture();

        // Start of Constructors region

        #region Constructors

        public ThumbnailWindowViewModel()
        {
            // Init messages
            WeakReferenceMessenger.Default.Register<TakeScreenshotMessage>(this, HandleTakeScreenshotMessage);
            WeakReferenceMessenger.Default.Register<UpdateScreenshotMessage>(this, HandleUpdateScreenshotMessage);
        }

        #endregion

        // Start of Events region

        #region Events

        public event EventHandler? Closing;
        public event EventHandler<BitmapSource>? ScreenshotUpdated;
        public event EventHandler<BitmapSource>? ScreenshotCreated;

        #endregion

        // Start of Properties region

        #region Properties

        public double ActualHeight { get; set; }
        public double ActualHeightPixels { get; set; }
        public double ActualWidth { get; set; }
        public double ActualWidthPixels { get; set; }

        public double Height
        {
            get => _height;
            set
            {
                if (SetProperty(ref _height, value))
                {
                    UpdateThumbnail();
                }
            }
        }

        public HWND Handle
        {
            get => _handle;
            set
            {
                SetProperty(ref _handle, value);
            }
        }

        public HWND HandleSource
        {
            get => _handleSource;
            set => SetProperty(ref _handleSource, value);
        }

        public double Left
        {
            get => _left;
            set => SetProperty(ref _left, value);
        }

        public int MouseX { get; set; } = 0;
        public int MouseY { get; set; } = 0;

        public double MouseXPercent
        {
            get
            {
                double mouseXPercent = 0;
                if (!_regionSource.IsEmpty)
                {
                    mouseXPercent = Math.Min((double)MouseX / _regionSource.Width * 100.0, 100.0);
                    mouseXPercent = Math.Max(mouseXPercent, 0.0);
                }
                return mouseXPercent;
            }
        }

        public double MouseYPercent
        {
            get
            {
                double mouseYPercent = 0;
                if (!_regionSource.IsEmpty)
                {
                    mouseYPercent = Math.Min((double)MouseY / _regionSource.Height * 100.0, 100.0);
                    mouseYPercent = Math.Max(mouseYPercent, 0.0);
                }
                return mouseYPercent;
            }
        }

        public int Opacity
        {
            get => 200;
        }

        public double Ratio
        {
            get
            {
                double xRatio = 16.0 / 9.0;
                if (!_regionSource.IsEmpty)
                {
                    xRatio = (double)_regionSource.Width / (double)_regionSource.Height;
                }
                return xRatio;
            }
        }

        public double Top
        {
            get => _top;
            set => SetProperty(ref _top, value);
        }

        public double Width
        {
            get => _width;
            set
            {
                if (SetProperty(ref _width, value))
                {
                    UpdateThumbnail();
                }
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        public void ClosingHandler()
        {
            Closing?.Invoke(this, EventArgs.Empty);
        }

        private void HandleTakeScreenshotMessage(object recipient, TakeScreenshotMessage message)
        {
            var bitmap = _screenCaptureGDI.GetScreenCapture(HandleSource);
            var bitmapSource = Helpers.ScreenCapture.ImageSourceFromBitmap(bitmap);
            bitmapSource?.Freeze();

            if (bitmapSource != null)
            {
                ScreenshotCreated?.Invoke(this, bitmapSource);
            }
        }

        private void HandleUpdateScreenshotMessage(object recipient, UpdateScreenshotMessage message)
        {
            var bitmap = _screenCaptureGDI.GetScreenCapture(HandleSource);
            var bitmapSource = Helpers.ScreenCapture.ImageSourceFromBitmap(bitmap);
            bitmapSource?.Freeze();

            if (bitmapSource != null)
            {
                ScreenshotUpdated?.Invoke(this, bitmapSource);
            }            
        }

        #endregion

        // Start of Methods region

        #region Methods

        public void Init()
        {
            RegisterThumbnail(HandleSource);
            _ = StartMouseTask();
        }

        public void RefreshThumbnailDestination()
        {
            if (_thumbnailHandle == IntPtr.Zero) return;

            SetThumbnailProperties(0, 0, (int)ActualWidthPixels, (int)ActualHeightPixels);
        }

        private System.Drawing.Size? GetSourceSize()
        {
            Debug.WriteLine($"GetSourceSize");

            // https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmquerythumbnailsourcesize
            HRESULT result = PInvoke.DwmQueryThumbnailSourceSize(_thumbnailHandle, out var sourceSize);
            if (result.Failed) return null;

            return new System.Drawing.Size(sourceSize.Width, sourceSize.Height);

            // Note: GetClientRect could be interesting when fSourceClientAreaOnly is set to true.
            //if (!PInvoke.GetClientRect(HandleSource, out RECT clientRect)) return null;
            //return new System.Drawing.Size(clientRect.right - clientRect.left, clientRect.bottom - clientRect.top);
        }

        private void RegisterThumbnail(HWND handleSource)
        {
            // https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmregisterthumbnail
            HRESULT result = PInvoke.DwmRegisterThumbnail(Handle, handleSource, out _thumbnailHandle);
            if (result.Failed) return;

            bool isRegionNotSet = _regionSource.IsEmpty;

            UpdateThumbnail();

            // Update DwmUpdateThumbnailProperties again to apply fSourceClientAreaOnly
            System.Drawing.Point sourcePoint = new System.Drawing.Point(0, 0);
            System.Drawing.Size? sourceSize = GetSourceSize();
            if (sourceSize == null) return;

            if (isRegionNotSet)
            {
                _regionSource = new RECT(sourcePoint, sourceSize.Value);
            }

            SetThumbnailProperties(0, 0, (int)ActualWidthPixels, (int)ActualHeightPixels);
        }

        private void SetThumbnailProperties(int left, int top, int right, int bottom)
        {
            // https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmupdatethumbnailproperties
            // https://learn.microsoft.com/en-us/windows/win32/dwm/dwm-tnp-constants
            DWM_THUMBNAIL_PROPERTIES DwmThumbnailProperties = new DWM_THUMBNAIL_PROPERTIES()
            {
                dwFlags = PInvoke.DWM_TNP_RECTDESTINATION | PInvoke.DWM_TNP_RECTSOURCE | PInvoke.DWM_TNP_OPACITY | PInvoke.DWM_TNP_VISIBLE | PInvoke.DWM_TNP_SOURCECLIENTAREAONLY,
                fSourceClientAreaOnly = false,
                fVisible = true,
                opacity = (byte)Opacity,
                rcDestination = new RECT
                {
                    left = left,
                    top = top,
                    right = right,
                    bottom = bottom,
                },
                rcSource = _regionSource
            };

            // https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmupdatethumbnailproperties
            HRESULT result = PInvoke.DwmUpdateThumbnailProperties(_thumbnailHandle, DwmThumbnailProperties);
            if (result.Failed) return;
        }

        private async Task StartMouseTask()
        {
            while (true)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        UpdateMouse();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error occurred while updating mouse position: {ex.Message}");
                        _delayUpdateMouse = 1000;
                    }
                });
                await Task.Delay(TimeSpan.FromMilliseconds(_delayUpdateMouse));
            }
        }

        private void UnRegisterThumbnail()
        {
            if (_thumbnailHandle == IntPtr.Zero) return;

            // https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmunregisterthumbnail
            HRESULT result = PInvoke.DwmUnregisterThumbnail(_thumbnailHandle);
            if (result.Failed) return;
        }

        private void UpdateMouse()
        {
            // Update offset values
            RECT region;
            PInvoke.GetWindowRect(HandleSource, out region);
            _offsetTop = region.top;
            _offsetLeft = region.left;

            // Note: ClientToScreen could be interesting when fSourceClientAreaOnly is set to true.
            //System.Drawing.Point clientOrigin = new System.Drawing.Point(0, 0);
            //PInvoke.ClientToScreen(HandleSource, ref clientOrigin);
            //_offsetTop = clientOrigin.Y;
            //_offsetLeft = clientOrigin.X;

            CURSORINFO cursorInfo = new CURSORINFO();
            cursorInfo.cbSize = (uint)Marshal.SizeOf(cursorInfo);
            PInvoke.GetCursorInfo(ref cursorInfo);

            //var monitor = PInvoke.User32.MonitorFromPoint(cursorInfo.ptScreenPos, PInvoke.User32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
            //var dpi = PInvoke.User32.GetDpiForMonitor(monitor, PInvoke.User32.MonitorDpiType.EFFECTIVE_DPI, out int dpiX, out int dpiY);
            var dpi = PInvoke.GetDpiForSystem();
            var dpiScaling = Math.Round(dpi / (double)96, 2);

            string mouseCoordinates = $"X: {cursorInfo.ptScreenPos.X}, Y: {cursorInfo.ptScreenPos.Y}";
            string mouseCoordinatesScaled = $"X: {(int)(cursorInfo.ptScreenPos.X / dpiScaling)}, Y: {(int)(cursorInfo.ptScreenPos.Y / dpiScaling)}";
            string mouseCoordinatesWindow = $"X: {cursorInfo.ptScreenPos.X - _offsetLeft}, Y: {cursorInfo.ptScreenPos.Y - _offsetTop}";
            string mouseCoordinatesWindowScaled = $"X: {(int)((cursorInfo.ptScreenPos.X - _offsetLeft) / dpiScaling)}, Y: {(int)((cursorInfo.ptScreenPos.Y - _offsetTop) / dpiScaling)}";

            WeakReferenceMessenger.Default.Send(new CursorUpdatedMessage(new CursorUpdatedMessageParams
            {
                X = cursorInfo.ptScreenPos.X - _offsetLeft,
                Y = cursorInfo.ptScreenPos.Y - _offsetTop
            }));

            MouseX = cursorInfo.ptScreenPos.X - _offsetLeft;
            MouseY = cursorInfo.ptScreenPos.Y - _offsetTop;

            //Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {mouseCoordinates}");
            //Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {mouseCoordinatesScaled} (SCALED)");
            //Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {mouseCoordinatesWindow} (WINDOW)");
            //Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name}: {mouseCoordinatesWindowScaled} (WINDOW) (SCALED)");

            _delayUpdateMouse = 25;
        }

        private void UpdateThumbnail()
        {
            if (_thumbnailHandle == IntPtr.Zero) return;

            System.Drawing.Point sourcePoint = new System.Drawing.Point(0, 0);
            System.Drawing.Size? sourceSize = GetSourceSize();
            if (sourceSize == null) return;

            if (_regionSource.IsEmpty)
            {
                _regionSource = new RECT(sourcePoint, sourceSize.Value);

                double boxWidth = Width;
                double boxHeight = Height;
                if (Ratio >= boxWidth / boxHeight)
                {
                    Height = boxWidth / Ratio;
                }
                else
                {
                    Width = boxHeight * Ratio;
                }
            }

            SetThumbnailProperties(0, 0, (int)ActualWidthPixels, (int)ActualHeightPixels);

            Debug.WriteLine($"UpdateThumbnail");
            Debug.WriteLine($"- Window: {Width}x{Height}");
            Debug.WriteLine($"- Window(Render): {ActualWidth}x{ActualHeight}");
            Debug.WriteLine($"- Window(Render pixels): {ActualWidthPixels}x{ActualHeightPixels}");
            Debug.WriteLine($"- Window(Source pixels): {sourceSize?.Width}x{sourceSize?.Height}");
        }        

        #endregion
    }
}
