using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Messages;
using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace D4Companion.ViewModels
{
    public class ThumbnailWindowViewModel : ObservableObject
    {
        private HWND _handle = HWND.Null;
        private HWND _handleSource = HWND.Null;
        private RECT _regionSource = new RECT();
        private IntPtr _thumbnailHandle = IntPtr.Zero;

        private double _height = 0;
        private double _left = 0;
        private double _top = 0;
        private double _width = 0;
        private int _opacity = 255;

        private ThumbnailPersistent? _thumbnailPersistent;
        private string _title = string.Empty;

        // Start of Constructors region

        #region Constructors

        public ThumbnailWindowViewModel()
        {
            // Init messages
            WeakReferenceMessenger.Default.Register<ThumbnailCloseRequestMessage>(this, HandleThumbnailCloseRequestMessage);
        }        

        #endregion

        // Start of Events region

        #region Events

        public event EventHandler? CloseRequest;
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

        public int Opacity
        {
            get => _opacity;
            set => SetProperty(ref _opacity, value);
        }      

        public double Top
        {
            get => _top;
            set => SetProperty(ref _top, value);
        }

        public ThumbnailPersistent? ThumbnailPersistent
        {
            get => _thumbnailPersistent;
            set
            {
                _thumbnailPersistent = value;
                OnPropertyChanged(nameof(ThumbnailPersistent));
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
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

        private void HandleThumbnailCloseRequestMessage(object recipient, ThumbnailCloseRequestMessage message)
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }      

        #endregion

        // Start of Methods region

        #region Methods

        public void Init()
        {
            if (ThumbnailPersistent == null) return;

            // Set source region
            _regionSource = new RECT(
                ThumbnailPersistent.SourcePositionX, 
                ThumbnailPersistent.SourcePositionY, 
                ThumbnailPersistent.SourcePositionX + ThumbnailPersistent.SourceWidth, 
                ThumbnailPersistent.SourcePositionY + ThumbnailPersistent.SourceHeight);

            // Set thumbnail
            RegisterThumbnail(HandleSource);
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
                //opacity = (byte)255,
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

            System.Drawing.Point sourcePoint = new System.Drawing.Point(0, 0);
            System.Drawing.Size? sourceSize = GetSourceSize();
            if (sourceSize == null) return;

            if (_regionSource.IsEmpty)
            {
                _regionSource = new RECT(sourcePoint, sourceSize.Value);
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
