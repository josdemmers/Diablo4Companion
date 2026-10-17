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
using D4Companion.Views;
using MahApps.Metro.Controls.Dialogs;
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
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ILogger _logger;

        private int? _badgeCount = null;
        private ScreenCapture _screenCaptureGDI = new ScreenCapture();
        private BitmapSource? _screenshotImage = null;
        private BitmapSource? _screenshotImageCached = null;
        private ThumbnailPersistentVM? _selectedThumbnailEdit = null;
        private string _thumbnailName = string.Empty;
        private bool _isDashboardOverlayEnabled = false;
        private bool _isDestinationROIActive = false;
        private bool _isShowAllROIsActive = false;
        private bool _isSourceROIActive = true;        

        private ObservableCollection<ThumbnailPersistent> _thumbnails = [];

        // Start of Constructors region

        #region Constructors

        public DashboardViewModel(ILogger<DashboardViewModel> logger, IDashboardManager dashboardManager, IDialogCoordinator dialogCoordinator)
        {
            // Init services
            _dashboardManager = dashboardManager;
            _dialogCoordinator = dialogCoordinator;
            _logger = logger;

            // Init messages
            WeakReferenceMessenger.Default.Register<ThumbnailROIUpdatedMessage>(this, HandleThumbnailROIUpdatedMessage);

            // Init view commands
            AddThumbnailCommand = new RelayCommand(AddThumbnailExecute, CanAddThumbnailExecute);
            RemoveThumbnailCommand = new RelayCommand<ThumbnailPersistent>(RemoveThumbnailExecute);
            SetSelectedThumbnailEditCommand = new RelayCommand<ThumbnailPersistent>(SetSelectedThumbnailEditExecute);
            SetSelectedThumbnailEditToggleCommand = new RelayCommand<ThumbnailPersistent>(SetSelectedThumbnailEditToggleExecute);
            ToggleDashboardOverlayCommand = new RelayCommand(ToggleDashboardOverlayExecute);
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

                ((RelayCommand)AddThumbnailCommand).NotifyCanExecuteChanged();
            }
        }

        public bool IsDestinationROIActive
        {
            get => _isDestinationROIActive;
            set
            {
                _isDestinationROIActive = value;
                _isSourceROIActive = !value;
                OnPropertyChanged(nameof(IsDestinationROIActive));
                OnPropertyChanged(nameof(IsSourceROIActive));
            }
        }

        public bool IsShowAllROIsActive
        {
            get => _isShowAllROIsActive;
            set
            {
                _isShowAllROIsActive = value;
                OnPropertyChanged(nameof(IsShowAllROIsActive));

                ScreenshotImage = DrawROIsOnBitmap(ScreenshotImageCached);
            }
        }

        public bool IsSourceROIActive 
        {
            get => _isSourceROIActive;
            set
            {
                _isDestinationROIActive = !value;
                _isSourceROIActive = value;
                OnPropertyChanged(nameof(IsDestinationROIActive));
                OnPropertyChanged(nameof(IsSourceROIActive));
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
            return !string.IsNullOrEmpty(ThumbnailName) && !Thumbnails.Any(t => t.Name == ThumbnailName) && !IsDashboardOverlayEnabled;
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
        }

        private void RemoveThumbnailExecute(ThumbnailPersistent? thumbnail)
        {
            _dialogCoordinator.ShowMessageAsync(this, 
                $"{TranslationSource.Instance["rsCapConfirmDelete"]}",
                $"{TranslationSource.Instance["rsMsgConfirmDeleteThumbnail"]} '{thumbnail?.Name ?? string.Empty}'?",
                MessageDialogStyle.AffirmativeAndNegative).ContinueWith(t =>
            {
                if (t.Result == MessageDialogResult.Affirmative)
                {
                    if (thumbnail == null) return;

                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        Thumbnails.Remove(thumbnail);
                        _dashboardManager.RemoveThumbnail(thumbnail);
                        UpdateThumbnails();
                    });                    
                }
            });

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

        private void ToggleDashboardOverlayExecute()
        {
            var process = Process.GetProcessesByName("Diablo IV").Where(p => p.MainWindowHandle != 0).FirstOrDefault();
            if (process == null)
            {
                IsDashboardOverlayEnabled = false;
                return;
            }

            IsDashboardOverlayEnabled = !IsDashboardOverlayEnabled;
            if (IsDashboardOverlayEnabled)
            {
                foreach (var thumbnail in Thumbnails.Where(t => t.IsEnabled))
                {
                    ThumbnailWindow thumbnailWindow = new ThumbnailWindow((HWND)process.MainWindowHandle, thumbnail);
                    thumbnailWindow.Show();
                }
            }
            else
            {
                  WeakReferenceMessenger.Default.Send(new ThumbnailCloseRequestMessage());
            }
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
                        // The selected destination (green) and source (red) ROIs are drawn by an overlay in the view
                        Rect rect;
                        Color strokeColor;
                        Pen pen;
                        double strokeThickness = 4;

                        // Others
                        if (IsShowAllROIsActive)
                        {
                            foreach (var thumbnail in Thumbnails)
                            {
                                if (thumbnail.Name == SelectedThumbnailEdit.Name) continue;

                                // Destination
                                rect = new Rect(thumbnail.DestinationPositionX, thumbnail.DestinationPositionY,
                                    thumbnail.DestinationWidth, thumbnail.DestinationHeight);
                                strokeColor = Colors.Blue;
                                pen = new Pen(new SolidColorBrush(strokeColor), strokeThickness);
                                drawingContext.DrawRectangle(null, pen, rect);

                                // Source
                                rect = new Rect(thumbnail.SourcePositionX, thumbnail.SourcePositionY,
                                    thumbnail.SourceWidth, thumbnail.SourceHeight);
                                strokeColor = Colors.Blue;
                                pen = new Pen(new SolidColorBrush(strokeColor), strokeThickness);
                                drawingContext.DrawRectangle(null, pen, rect);
                            }
                        }
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
