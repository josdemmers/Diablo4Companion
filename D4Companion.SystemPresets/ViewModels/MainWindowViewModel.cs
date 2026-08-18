using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Extensions;
using D4Companion.SystemPresets.Entities;
using D4Companion.SystemPresets.Interfaces;
using D4Companion.SystemPresets.Messages;
using D4Companion.SystemPresets.ViewModels.Entities;
using D4Companion.SystemPresets.Views;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace D4Companion.SystemPresets.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger _logger;
        private readonly IScreenManager _screenManager;
        private readonly ISystemPresetManager _systemPresetManager;

        private ObservableCollection<IconType> _iconTypes = [];
        private ObservableCollection<ScreenCaptureVM> _screenCaptures = [];
        private ObservableCollection<SystemPreset> _systemPresets = [];

        private string _coordinates = string.Empty;
        private BitmapSource? _iconPreview = null;
        private BitmapSource? _iconTypeScreenshot = null;
        private BitmapSource? _iconTypeScreenshotCached = null;
        private bool _isLiveModeActive = true;
        private IconType _selectedIconType = new IconType();
        private IconTypeVM? _selectedIconTypeEdit = null;
        private SystemPreset _selectedSystemPreset = new SystemPreset();
        private string _systemPresetName = string.Empty;
        private string _windowTitle = $"Diablo IV Companion - System Presets v{Assembly.GetExecutingAssembly().GetName().Version}";

        // Start of Constructors region

        #region Constructors

        public MainWindowViewModel(ILogger<MainWindowViewModel> logger, IScreenManager screenManager, ISystemPresetManager systemPresetManager)
        {
            // Init services
            _logger = logger;
            _screenManager = screenManager;
            _systemPresetManager = systemPresetManager;

            // Init messages
            WeakReferenceMessenger.Default.Register<ActiveScreenChangedMessage>(this, HandleActiveScreenChangedMessage);
            WeakReferenceMessenger.Default.Register<CursorUpdatedMessage>(this, HandleCursorUpdatedMessage);
            WeakReferenceMessenger.Default.Register<IconTypeROIUpdatedMessage>(this, HandleIconTypeROIUpdatedMessage);
            WeakReferenceMessenger.Default.Register<ScreenAddedMessage>(this, HandleScreenAddedMessage);
            WeakReferenceMessenger.Default.Register<ScreenUpdatedMessage>(this, HandleScreenUpdatedMessage);
            WeakReferenceMessenger.Default.Register<SystemPresetsUpdatedMessage>(this, HandleSystemPresetsUpdatedMessage);

            // Init view commands
            ApplicationClosingCommand = new RelayCommand(ApplicationClosingExecute);
            ApplicationLoadedCommand = new RelayCommand(ApplicationLoadedExecute);
            AddSelectedIconTypeCommand = new RelayCommand(AddSelectedIconTypeExecute, CanAddSelectedIconTypeExecute);
            AddSystemPresetCommand = new RelayCommand(AddSystemPresetExecute, CanAddSystemPresetExecute);
            ApplySelectedIconTypeChangesCommand = new RelayCommand(ApplySelectedIconTypeChangesExecute, CanApplySelectedIconTypeChangesExecute);
            RemoveIconTypeCommand = new RelayCommand<IconType>(RemoveIconTypeExecute);
            RemoveScreenshotCommand = new RelayCommand(RemoveScreenshotExecute, CanRemoveScreenshotExecute);
            RemoveScreenshotAllCommand = new RelayCommand(RemoveScreenshotAllExecute, CanRemoveScreenshotAllExecute);
            RemoveSystemPresetCommand = new RelayCommand(RemoveSystemPresetExecute, CanRemoveSystemPresetExecute);
            SaveIconTypeROIsCommand = new RelayCommand(SaveIconTypeROIsExecute, CanSaveIconTypeROIsExecute);
            SetSelectedIconTypeEditCommand = new RelayCommand<IconType>(SetSelectedIconTypeEditExecute);
            ShowIconPreviewCommand = new RelayCommand(ShowIconPreviewExecute);
            SwitchImageModeCommand = new RelayCommand(SwitchImageModeExecute, CanSwitchImageModeExecute);
            TakeScreenshotCommand = new AsyncRelayCommand(TakeScreenshotExecute, CanTakeScreenshotExecute);
            UpdateScreenshotCommand = new AsyncRelayCommand(UpdateScreenshotExecute, CanUpdateScreenshotExecute);

            // Init
            InitIconTypes();
        }        

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<IconType> IconTypes { get => _iconTypes; set => _iconTypes = value; }
        public ObservableCollection<ScreenCaptureVM> ScreenCaptures { get => _screenCaptures; set => _screenCaptures = value; }
        public ObservableCollection<SystemPreset> SystemPresets { get => _systemPresets; set => _systemPresets = value; }

        public ICommand AddSelectedIconTypeCommand { get; }
        public ICommand ApplicationClosingCommand { get; }
        public ICommand ApplicationLoadedCommand { get; }        
        public ICommand AddSystemPresetCommand { get; }
        public ICommand ApplySelectedIconTypeChangesCommand { get; }
        public ICommand RemoveIconTypeCommand { get; }
        public ICommand RemoveScreenshotCommand { get; }
        public ICommand RemoveScreenshotAllCommand { get; }
        public ICommand RemoveSystemPresetCommand { get; }
        public ICommand SaveIconTypeROIsCommand { get; }
        public ICommand SetSelectedIconTypeEditCommand { get; }
        public ICommand ShowIconPreviewCommand {  get; }
        public ICommand SwitchImageModeCommand { get; }
        public ICommand TakeScreenshotCommand { get; }
        public ICommand UpdateScreenshotCommand { get; }

        public string Coordinates
        {
            get
            {
                return _coordinates;
            }
            set
            {
                _coordinates = value;
                OnPropertyChanged(nameof(Coordinates));
            }
        }

        public BitmapSource? IconPreview
        {
            get => _iconPreview;
            set
            {
                _iconPreview = value;
                OnPropertyChanged(nameof(IconPreview));
            }
        }

        public BitmapSource? IconTypeScreenCapture
        {
            get
            {
                return SelectedScreenCapture?.BitmapSource;
            }
        }


        public BitmapSource? IconTypeScreenshot
        {
            get
            {
                return _iconTypeScreenshot;
            }
            set
            {
                _iconTypeScreenshot = value;
                OnPropertyChanged(nameof(IconTypeScreenshot));
            }
        }

        public BitmapSource? IconTypeScreenshotCached
        {
            get
            {
                return _iconTypeScreenshotCached;
            }
            set
            {
                _iconTypeScreenshotCached = value;
                OnPropertyChanged(nameof(IconTypeScreenshotCached));
            }
        }

        public bool IsLiveModeActive
        {
            get => _isLiveModeActive;
            set
            {
                _isLiveModeActive = value;
                OnPropertyChanged();
            }
        }

        public List<string> Screenshots
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedSystemPreset?.Name))
                {
                    return new List<string>();
                }

                string systemPresetsPath = Path.Combine(
                    ".",
                    "SystemPresets",
                    SelectedSystemPreset.Name
                );

                if (!Directory.Exists(systemPresetsPath))
                {
                    return new List<string>();
                }

                return Directory.GetFiles(systemPresetsPath, "*.png", SearchOption.TopDirectoryOnly).ToList();
            }
        }

        public IconType SelectedIconType
        {
            get => _selectedIconType;
            set
            {
                _selectedIconType = value;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ((RelayCommand)AddSelectedIconTypeCommand).NotifyCanExecuteChanged();
                });
            }
        }

        public IconTypeVM? SelectedIconTypeEdit
        {
            get => _selectedIconTypeEdit;
            set
            {
                _selectedIconTypeEdit = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedIconTypeEdit));

                if (SelectedIconTypeEdit != null)
                {
                    if (string.IsNullOrWhiteSpace(SelectedIconTypeEdit?.SelectedScreenshot))
                    {
                        SelectedIconTypeEdit?.SelectedScreenshot = Screenshots.FirstOrDefault() ?? string.Empty;
                    }
                }
                
                LoadSelectedScreenshot();

                OnPropertyChanged(nameof(IconTypeScreenCapture));
                OnPropertyChanged(nameof(SelectedScreenshot));                
                OnPropertyChanged(nameof(Screenshots));                

                ((RelayCommand)ApplySelectedIconTypeChangesCommand).NotifyCanExecuteChanged();
                ((RelayCommand)RemoveScreenshotCommand).NotifyCanExecuteChanged();
                ((RelayCommand)RemoveScreenshotAllCommand).NotifyCanExecuteChanged();
                ((RelayCommand)SaveIconTypeROIsCommand).NotifyCanExecuteChanged();
                ((RelayCommand)SwitchImageModeCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)TakeScreenshotCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)UpdateScreenshotCommand).NotifyCanExecuteChanged();
            }
        }

        public string SelectedScreenshot
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedIconTypeEdit?.Name))
                {
                    return string.Empty;
                }

                return SelectedIconTypeEdit.SelectedScreenshot;
            }
            set
            {
                SelectedIconTypeEdit?.SelectedScreenshot = value;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ((RelayCommand)RemoveScreenshotCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)RemoveScreenshotAllCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)UpdateScreenshotCommand).NotifyCanExecuteChanged();
                });

                LoadSelectedScreenshot();
            }
        }

        public SystemPreset SelectedSystemPreset
        { 
            get => _selectedSystemPreset;
            set
            {
                _selectedSystemPreset = value;
                OnPropertyChanged();                

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ((RelayCommand)RemoveSystemPresetCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)AddSelectedIconTypeCommand).NotifyCanExecuteChanged();
                });
            }
        }

        public string SystemPresetName 
        { 
            get => _systemPresetName;
            set 
            { 
                _systemPresetName = value;
                OnPropertyChanged(nameof(SystemPresetName));

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ((RelayCommand)AddSystemPresetCommand).NotifyCanExecuteChanged();
                });
            } 
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public ScreenCaptureVM? SelectedScreenCapture
        {
            get
            {
                return _screenCaptures.FirstOrDefault(s => s.IsActive);
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void ApplicationClosingExecute()
        {
            WeakReferenceMessenger.Default.Send(new ApplicationClosingMessage());
        }

        private void ApplicationLoadedExecute()
        {
            _logger.LogInformation(WindowTitle);

            WeakReferenceMessenger.Default.Send(new ApplicationLoadedMessage());
        }

        private void HandleActiveScreenChangedMessage(object recipient, ActiveScreenChangedMessage message)
        {
            var activeScreenChangedMessage = message.Value;

            foreach (var screenCapture in ScreenCaptures)
            {
                if (screenCapture.DeviceName == activeScreenChangedMessage.DeviceName) continue;
                if (!screenCapture.IsActive || !activeScreenChangedMessage.IsActive) continue;

                screenCapture.IsActive = false;
            }
        }

        private void HandleCursorUpdatedMessage(object recipient, CursorUpdatedMessage message)
        {
            var cursorUpdatedMessage = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Coordinates = $"({cursorUpdatedMessage.X}, {cursorUpdatedMessage.Y})";
            });
        }

        private void HandleIconTypeROIUpdatedMessage(object recipient, IconTypeROIUpdatedMessage message)
        {
            IconTypeScreenshot = DrawROIsOnBitmap(IconTypeScreenshotCached);            
        }        

        private void HandleScreenAddedMessage(object recipient, ScreenAddedMessage message)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ScreenCaptures.Clear();
                foreach (var screenCapture in _screenManager.ScreenCaptures)
                {
                    ScreenCaptures.Add(new ScreenCaptureVM(screenCapture));
                }               
            });
        }

        private void HandleScreenUpdatedMessage(object recipient, ScreenUpdatedMessage message)
        {
            foreach (var screenCapture in ScreenCaptures)
            {
                screenCapture.Update();
            }
            OnPropertyChanged(nameof(IconTypeScreenCapture));
        }

        private void HandleSystemPresetsUpdatedMessage(object recipient, SystemPresetsUpdatedMessage message)
        {
            UpdateSystemPresets();
        }

        private bool CanAddSelectedIconTypeExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedSystemPreset?.Name) && !string.IsNullOrWhiteSpace(SelectedIconType?.Name);
        }

        private void AddSelectedIconTypeExecute()
        {
            int count = SelectedSystemPreset.IconTypes.Count(preset => preset.Name.Equals(SelectedIconType.Name));
            SelectedSystemPreset.IconTypes.Add(new IconType
            {
                DisplayName = SelectedIconType.DisplayName,
                Name = SelectedIconType.Name,                
                Count = count + 1
            });            

            _systemPresetManager.Save(SelectedSystemPreset);

            UpdateSystemPresets();
        }

        private bool CanAddSystemPresetExecute()
        {
            return !string.IsNullOrWhiteSpace(SystemPresetName) &&
                !_systemPresets.Any(preset => preset.Name.Equals(SystemPresetName));
        }

        private void AddSystemPresetExecute()
        {
            _systemPresetManager.AddSystemPreset(SystemPresetName);
            SystemPresetName = string.Empty;

            UpdateSystemPresets();
        }

        private bool CanApplySelectedIconTypeChangesExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedIconTypeEdit?.Name);
        }

        private void ApplySelectedIconTypeChangesExecute()
        {
            _systemPresetManager.Save(SelectedSystemPreset);
        }

        private void RemoveIconTypeExecute(IconType? type)
        {
            if (type == null) return;

            SelectedSystemPreset.IconTypes.Remove(type);
            _systemPresetManager.Save(SelectedSystemPreset);
            UpdateSystemPresets();
        }

        private bool CanRemoveScreenshotExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedScreenshot);
        }

        private void RemoveScreenshotExecute()
        {
            if(File.Exists(SelectedScreenshot))
            {
                File.Delete(SelectedScreenshot);
                OnPropertyChanged(nameof(Screenshots));
            }            
        }

        private bool CanRemoveScreenshotAllExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedScreenshot);
        }

        private void RemoveScreenshotAllExecute()
        {
            foreach (var screenshot in Screenshots)
            {
                bool inUse = SelectedSystemPreset.IconTypes.Any(iconType => iconType.SelectedScreenshot.Equals(screenshot));

                if (File.Exists(screenshot) && !inUse)
                {
                    File.Delete(screenshot);
                }
            }
            OnPropertyChanged(nameof(Screenshots));
        }

        private bool CanRemoveSystemPresetExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedSystemPreset?.Name);
        }

        private void RemoveSystemPresetExecute()
        {
            _systemPresetManager.RemoveSystemPreset(SelectedSystemPreset.Name);
        }

        private bool CanSaveIconTypeROIsExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedSystemPreset?.Name);
        }

        private void SaveIconTypeROIsExecute()
        {
            foreach (var iconType in SelectedSystemPreset.IconTypes)
            {
                if(!File.Exists(iconType.SelectedScreenshot)) continue;

                using (var stream = new FileStream(iconType.SelectedScreenshot, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    var rect = new Int32Rect(iconType.PositionX, iconType.PositionY, iconType.Width, iconType.Height);
                    var cropped = new CroppedBitmap(bitmap, rect);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(cropped));

                    string outputDir = Path.Combine(".", "SystemPresets", SelectedSystemPreset.Name, "Images");
                    if (iconType.Name.StartsWith("tooltip"))
                    {
                        outputDir = Path.Combine(".", "SystemPresets", SelectedSystemPreset.Name, "Images", "Tooltips");
                    }                    
                    Directory.CreateDirectory(outputDir);

                    string fileName = iconType.Count > 1
                        ? $"{iconType.Name}_{iconType.Count}.png"
                        : $"{iconType.Name}.png";

                    string outputPath = Path.Combine(outputDir, fileName);
                    using (var fileStream = new FileStream(outputPath, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }
                }
            }
        }

        private void SetSelectedIconTypeEditExecute(IconType? iconType)
        {
            if (iconType != null)
            {
                SelectedIconTypeEdit = new IconTypeVM(iconType);
            }
        }

        private void ShowIconPreviewExecute()
        {
            if (IconPreview == null) return;

            var iconPreview = new IconPreviewWindow
            {
                DataContext = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            iconPreview.Show();
        }

        private bool CanSwitchImageModeExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedSystemPreset?.Name);
        }

        private void SwitchImageModeExecute()
        {
            IsLiveModeActive = !IsLiveModeActive;
        }

        private bool CanTakeScreenshotExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedSystemPreset?.Name);
        }

        private async Task TakeScreenshotExecute()
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            
            if (IconTypeScreenCapture != null)
            {
                _systemPresetManager.SaveScreenshot(IconTypeScreenCapture, SelectedSystemPreset.Name);
                OnPropertyChanged(nameof(Screenshots));
            }
        }

        private bool CanUpdateScreenshotExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedScreenshot);
        }

        private async Task UpdateScreenshotExecute()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));

            if (IconTypeScreenCapture != null)
            {
                string oldScreenshot = SelectedScreenshot;
                string updatedScreenshot = _systemPresetManager.UpdateScreenshot(IconTypeScreenCapture, SelectedSystemPreset.Name, SelectedScreenshot);

                // Update icons to use the new screenshot
                foreach (var iconType in SelectedSystemPreset.IconTypes)
                {
                    if (iconType.SelectedScreenshot.Equals(oldScreenshot))
                    {
                        iconType.SelectedScreenshot = updatedScreenshot;
                    }
                }
                _systemPresetManager.Save(SelectedSystemPreset);
                OnPropertyChanged(nameof(Screenshots));
                OnPropertyChanged(nameof(SelectedScreenshot));
                LoadSelectedScreenshot();
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        private BitmapSource? DrawROIsOnBitmap(BitmapSource? source)
        {
            if (source == null) return null;

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Draw original image
                drawingContext.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));

                // Draw ROIs
                foreach (var iconType in SelectedSystemPreset.IconTypes)
                {
                    if (!iconType.SelectedScreenshot.Equals(SelectedScreenshot)) continue;
                    if (string.IsNullOrWhiteSpace(SelectedIconTypeEdit?.Name)) continue;

                    Rect rect = new Rect(iconType.PositionX, iconType.PositionY, iconType.Width, iconType.Height);
                    Color strokeColor = ReferenceEquals(iconType, SelectedIconTypeEdit!.Model) ? Colors.Green : Colors.Red;
                    double strokeThickness = 2;
                    var pen = new Pen(new SolidColorBrush(strokeColor), strokeThickness);
                    drawingContext.DrawRectangle(null, pen, rect);
                }

                UpdateIconPreview();
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


        private void InitIconTypes()
        {
            IconTypes.Clear();
            IconTypes.AddRange(_systemPresetManager.GetItemTypes());
        }

        private void LoadSelectedScreenshot()
        {
            if (string.IsNullOrWhiteSpace(SelectedScreenshot))
            {
                return;
            }
            if (!File.Exists(SelectedScreenshot))
            {
                return;
            }
            try
            {
                using (var stream = new FileStream(SelectedScreenshot, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    IconTypeScreenshotCached = bitmap;
                    IconTypeScreenshot = DrawROIsOnBitmap(bitmap);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load screenshot: {SelectedScreenshot}");
            }
        }

        private void UpdateIconPreview()
        {
            if (IconTypeScreenshotCached != null && SelectedIconTypeEdit != null)
            {
                var region = new Int32Rect(SelectedIconTypeEdit.PositionX, SelectedIconTypeEdit.PositionY, SelectedIconTypeEdit.Width, SelectedIconTypeEdit.Height);

                BitmapSource cropped = new CroppedBitmap(IconTypeScreenshotCached, region);
                cropped.Freeze();
                IconPreview = cropped;
            }
        }

        private void UpdateSystemPresets()
        {
            string systemPresetName = SelectedSystemPreset.Name;

            SystemPresets.Clear();
            SystemPresets.AddRange(_systemPresetManager.SystemPresets);

            if (!string.IsNullOrWhiteSpace(systemPresetName))
            {
                SelectedSystemPreset = SystemPresets.FirstOrDefault(preset => preset.Name.Equals(systemPresetName)) ?? new();
            }
            else
            {
                SelectedSystemPreset = SystemPresets?.FirstOrDefault() ?? new();
            }
        }

        #endregion
    }
}
