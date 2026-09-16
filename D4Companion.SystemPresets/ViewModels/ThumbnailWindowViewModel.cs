using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Media.Media3D;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace D4Companion.SystemPresets.ViewModels
{
    public class ThumbnailWindowViewModel : ObservableObject
    {
        private HWND _handle = HWND.Null;
        private HWND _handleSource = HWND.Null;
        private RECT _regionSource = new RECT();
        private IntPtr _thumbnailHandle = IntPtr.Zero;

        private int _height = 720;
        private double _left = 0;
        private double _top = 0;
        private int _width = 1280;                

        // Start of Constructors region

        #region Constructors

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public double ActualHeight { get; set; }
        public double ActualHeightPixels { get; set; }
        public double ActualWidth { get; set; }
        public double ActualWidthPixels { get; set; }

        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged(nameof(Height));

                UpdateThumbnail();
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
            set
            {
                _left = value;
                OnPropertyChanged(nameof(Left));
            }
        }

        public int Opacity
        {
            // TODO: Maybe need to tweak this to be able to see mouse cursor in the thumbnail window.
            get => 200; // 255            
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
            set
            {
                _top = value;
                OnPropertyChanged(nameof(Top));
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged(nameof(Width));

                UpdateThumbnail();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        public void Init()
        {
            RegisterThumbnail(HandleSource);
        }

        private System.Drawing.Size? GetSourceSize()
        {
            Debug.WriteLine($"GetSourceSize");

            // https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmquerythumbnailsourcesize
            HRESULT result = PInvoke.DwmQueryThumbnailSourceSize(_thumbnailHandle, out var sourceSize);
            if (result.Failed) return null;

            return new System.Drawing.Size(sourceSize.Width, sourceSize.Height);
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
                //ThumbnailConfigViewModel.RegionSource = new RegionSourceRECT
                //{
                //    Left = _regionSource.left,
                //    Top = _regionSource.top,
                //    Right = _regionSource.right,
                //    Bottom = _regionSource.bottom
                //};
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
                fSourceClientAreaOnly = true,
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

        private void UnRegisterThumbnail()
        {
            if (_thumbnailHandle == IntPtr.Zero) return;

            // https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmunregisterthumbnail
            HRESULT result = PInvoke.DwmUnregisterThumbnail(_thumbnailHandle);
            if (result.Failed) return;
        }

        private void UpdateThumbnail()
        {
            if (_thumbnailHandle == IntPtr.Zero) return;

            //RECT rectDestination = GetExtendedFrameBounds(Handle);
            System.Drawing.Point sourcePoint = new System.Drawing.Point(0, 0);
            System.Drawing.Size? sourceSize = GetSourceSize();
            if (sourceSize == null) return;

            if (_regionSource.IsEmpty)
            {
                _regionSource = new RECT(sourcePoint, sourceSize.Value);
                //ThumbnailConfigViewModel.RegionSource = new RegionSourceRECT
                //{
                //    Left = _regionSource.left,
                //    Top = _regionSource.top,
                //    Right = _regionSource.right,
                //    Bottom = _regionSource.bottom
                //};
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
