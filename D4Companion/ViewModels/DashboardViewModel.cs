using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Extensions;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using D4Companion.Localization;
using D4Companion.Messages;
using D4Companion.ViewModels.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Win32.Foundation;

namespace D4Companion.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly IDashboardManager _dashboardManager;
        private readonly ILogger _logger;

        private int? _badgeCount = null;
        private ScreenCapture _screenCaptureGDI = new ScreenCapture();
        private BitmapSource? _screenshotImage = null;
        private BitmapSource? _screenshotImageCached = null;
        private ThumbnailPersistentVM? _selectedThumbnailEdit = null;
        private string _thumbnailName = string.Empty;
        private bool _isDashboardOverlayEnabled = false;
        

        private ObservableCollection<ThumbnailPersistent> _thumbnails = [];

        // Start of Constructors region

        #region Constructors

        public DashboardViewModel(ILogger<DashboardViewModel> logger, IDashboardManager dashboardManager)
        {
            // Init services
            _dashboardManager = dashboardManager;
            _logger = logger;

            // Init messages
            WeakReferenceMessenger.Default.Register<ThumbnailROIUpdatedMessage>(this, HandleThumbnailROIUpdatedMessage);

            // Init view commands
            AddThumbnailCommand = new RelayCommand(AddThumbnailExecute, CanAddThumbnailExecute);
            RemoveThumbnailCommand = new RelayCommand<ThumbnailPersistent>(RemoveThumbnailExecute);
            SetSelectedThumbnailEditCommand = new RelayCommand<ThumbnailPersistent>(SetSelectedThumbnailEditExecute);
            SetSelectedThumbnailEditToggleCommand = new RelayCommand<ThumbnailPersistent>(SetSelectedThumbnailEditToggleExecute);
            ToggleDashboardOverlayCommand = new RelayCommand<bool?>(ToggleDashboardOverlayExecute);
            UpdateScreenshotCommand = new RelayCommand(UpdateScreenshotExecute);

            // Init data
            InitScreenshot();
            InitThumbnails();
        }        

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand AddThumbnailCommand { get; }
        public ICommand RemoveThumbnailCommand { get; }
        public ICommand SetSelectedThumbnailEditCommand { get; }
        public ICommand SetSelectedThumbnailEditToggleCommand { get; }

        public ICommand ToggleDashboardOverlayCommand { get; }
        public ICommand UpdateScreenshotCommand { get; }

        public ObservableCollection<ThumbnailPersistent> Thumbnails { get => _thumbnails; set => _thumbnails = value; }

        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }

        public bool IsDashboardOverlayEnabled
        {
            get => _isDashboardOverlayEnabled;
            set
            {
                _isDashboardOverlayEnabled = value;
                OnPropertyChanged(nameof(IsDashboardOverlayEnabled));
            }
        }

        public bool IsSelectedThumbnailSet
        {
            get => SelectedThumbnailEdit != null;
        }

        public BitmapSource? ScreenshotImage
        {
            get
            {
                return _screenshotImage;
            }
            set
            {
                _screenshotImage = value;
                OnPropertyChanged(nameof(ScreenshotImage));
            }
        }

        public BitmapSource? ScreenshotImageCached
        {
            get
            {
                return _screenshotImageCached;
            }
            set
            {
                _screenshotImageCached = value;
                OnPropertyChanged(nameof(ScreenshotImageCached));
            }
        }

        public ThumbnailPersistentVM? SelectedThumbnailEdit
        {
            get => _selectedThumbnailEdit;
            set
            {
                _selectedThumbnailEdit = value;
                OnPropertyChanged(nameof(SelectedThumbnailEdit));
                OnPropertyChanged(nameof(IsSelectedThumbnailSet));

                ScreenshotImage = DrawROIsOnBitmap(ScreenshotImageCached);
            }
        }

        public string ThumbnailName
        {
            get => _thumbnailName;
            set
            {
                _thumbnailName = value;
                OnPropertyChanged(nameof(ThumbnailName));

                ((RelayCommand)AddThumbnailCommand).NotifyCanExecuteChanged();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private bool CanAddThumbnailExecute()
        {
            return !string.IsNullOrEmpty(ThumbnailName) && !Thumbnails.Any(t => t.Name == ThumbnailName);
        }

        private void AddThumbnailExecute()
        {
            var thumbnail = new ThumbnailPersistent
            {
                Name = ThumbnailName,
            };

            Thumbnails.Add(thumbnail);
            _dashboardManager.AddThumbnail(thumbnail);
            UpdateThumbnails();

            ((RelayCommand)AddThumbnailCommand).NotifyCanExecuteChanged();
        }

        private void HandleThumbnailROIUpdatedMessage(object recipient, ThumbnailROIUpdatedMessage message)
        {
            _dashboardManager.SaveThumbnails();
            ScreenshotImage = DrawROIsOnBitmap(ScreenshotImageCached);
        }

        private void RemoveThumbnailExecute(ThumbnailPersistent? thumbnail)
        {
            var result = MessageBox.Show(
                $"{TranslationSource.Instance["rsMsgConfirmDeleteThumbnail"]} '{thumbnail?.Name ?? string.Empty}'?",
                $"{TranslationSource.Instance["rsCapConfirmDelete"]}",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (thumbnail == null) return;

                Thumbnails.Remove(thumbnail);
                _dashboardManager.RemoveThumbnail(thumbnail);
                UpdateThumbnails();
            }

            ((RelayCommand)AddThumbnailCommand).NotifyCanExecuteChanged();
        }

        private void SetSelectedThumbnailEditExecute(ThumbnailPersistent? thumbnail)
        {
            if (thumbnail == null) return;

            SelectedThumbnailEdit = new ThumbnailPersistentVM(thumbnail);
        }

        private void SetSelectedThumbnailEditToggleExecute(ThumbnailPersistent? thumbnail)
        {
            if (thumbnail == null) return;

            thumbnail.IsEnabled = !thumbnail.IsEnabled;
            _dashboardManager.SaveThumbnails();

            UpdateThumbnails();
        }

        private void ToggleDashboardOverlayExecute(bool? isEnabled)
        {

        }

        private void UpdateScreenshotExecute()
        {
            var process = Process.GetProcessesByName("Diablo IV").Where(p => p.MainWindowHandle != 0).FirstOrDefault();
            if (process == null) return;

            var bitmap = _screenCaptureGDI.GetScreenCapture((HWND)process.MainWindowHandle);
            var bitmapSource = ScreenCapture.ImageSourceFromBitmap(bitmap);
            bitmapSource?.Freeze();
            if (bitmapSource == null) return;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            string filePath = @$".\Images\Dashboard\Screenshot.png";
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }

            InitScreenshot();
        }

        #endregion

        // Start of Methods region

        #region Methods

        private BitmapSource? DrawROIsOnBitmap(BitmapSource? source)
        {            
            if (source == null) return null;

            try
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    // Draw original image
                    drawingContext.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));

                    // Draw ROIs
                    if (SelectedThumbnailEdit != null)
                    {
                        // Destination
                        Rect rect = new Rect(SelectedThumbnailEdit.DestinationPositionX, SelectedThumbnailEdit.DestinationPositionY, 
                            SelectedThumbnailEdit.DestinationWidth, SelectedThumbnailEdit.DestinationHeight);
                        Color strokeColor = Colors.Green;
                        double strokeThickness = 2;
                        var pen = new Pen(new SolidColorBrush(strokeColor), strokeThickness);
                        drawingContext.DrawRectangle(null, pen, rect);

                        // Source
                        rect = new Rect(SelectedThumbnailEdit.SourcePositionX, SelectedThumbnailEdit.SourcePositionY,
                            SelectedThumbnailEdit.SourceWidth, SelectedThumbnailEdit.SourceHeight);
                        strokeColor = Colors.Red;
                        pen = new Pen(new SolidColorBrush(strokeColor), strokeThickness);
                        drawingContext.DrawRectangle(null, pen, rect);
                    }
                }

                var renderTargetBitmap = new RenderTargetBitmap(
                    source.PixelWidth,
                    source.PixelHeight,
                    source.DpiX,
                    source.DpiY,
                    PixelFormats.Pbgra32);

                renderTargetBitmap.Render(drawingVisual);
                renderTargetBitmap.Freeze(); // Freeze the bitmap to make it cross-thread accessible
                return renderTargetBitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to draw ROI on screenshot.");
            }

            return source;
        }

        private void InitScreenshot()
        {
            string filePath = @$".\Images\Dashboard\Screenshot.png";
            if (!File.Exists(filePath)) return;

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var bitmapGUI = new BitmapImage();
                bitmapGUI.BeginInit();
                bitmapGUI.CacheOption = BitmapCacheOption.OnLoad;
                bitmapGUI.StreamSource = stream;
                bitmapGUI.EndInit();
                bitmapGUI.Freeze();

                ScreenshotImageCached = bitmapGUI;
                ScreenshotImage = DrawROIsOnBitmap(bitmapGUI);
            }
        }

        private void InitThumbnails()
        {
            Thumbnails.Clear();
            Thumbnails.AddRange(_dashboardManager.Thumbnails);

            if (Thumbnails.Any())
            {
                SelectedThumbnailEdit = new ThumbnailPersistentVM(Thumbnails.First());
            }
        }

        private void UpdateThumbnails()
        {
            string? thumbnailName = SelectedThumbnailEdit?.Name;

            Thumbnails.Clear();
            Thumbnails.AddRange(_dashboardManager.Thumbnails);

            if (!string.IsNullOrEmpty(thumbnailName) && Thumbnails.Any(t => t.Name == thumbnailName))
            {
                var thumbnail = Thumbnails.First(t => t.Name == thumbnailName);
                SelectedThumbnailEdit = new ThumbnailPersistentVM(thumbnail);
            }
            else
            {
                if (Thumbnails.Any())
                {
                    SelectedThumbnailEdit = new ThumbnailPersistentVM(Thumbnails.First());
                }
                else
                {
                    SelectedThumbnailEdit = null;
                }
            }
        }

        #endregion        
    }
}
