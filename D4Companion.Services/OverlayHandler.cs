using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Threading;

namespace D4Companion.Services
{
    public class OverlayHandler : IOverlayHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private GraphicsWindow? _window = null;
        private readonly Dictionary<string, SolidBrush> _brushes = new Dictionary<string, SolidBrush>();
        private readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();
        private readonly Dictionary<string, Image> _images = new Dictionary<string, Image>();

        private string _currentAffixPreset = string.Empty;
        private bool _currentAffixPresetVisible = false;
        private DispatcherTimer _currentAffixPresetTimer = new();
        private ItemTooltipDescriptor _currentTooltip = new ItemTooltipDescriptor();
        private object _lockItemTooltip = new object();
        private object _lockWindowHandle = new object();
        private List<OverlayMenuItem> _overlayMenuItems = new List<OverlayMenuItem>();
        IntPtr _windowHandle = IntPtr.Zero;

        // Start of Constructors region

        #region Constructors

        public OverlayHandler(IEventAggregator eventAggregator, ILogger<ScreenProcessHandler> logger, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AffixPresetChangedEvent>().Subscribe(HandleAffixPresetChangedEvent);
            _eventAggregator.GetEvent<MenuLockedEvent>().Subscribe(HandleMenuLockedEvent);
            _eventAggregator.GetEvent<MenuUnlockedEvent>().Subscribe(HandleMenuUnlockedEvent);
            _eventAggregator.GetEvent<ToggleOverlayFromGUIEvent>().Subscribe(HandleToggleOverlayFromGUIEvent);
            _eventAggregator.GetEvent<TooltipDataReadyEvent>().Subscribe(HandleTooltipDataReadyEvent);
            _eventAggregator.GetEvent<WindowHandleUpdatedEvent>().Subscribe(HandleWindowHandleUpdatedEvent);
            
            // Init logger
            _logger = logger;

            // Init services
            _settingsManager = settingsManager;

            // Init overlay objects
            InitOverlayObjects();

            // Init timer
            _currentAffixPresetTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000),
                IsEnabled = false
            };
            _currentAffixPresetTimer.Tick += CurrentAffixPresetTimer_Tick;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void CurrentAffixPresetTimer_Tick(object? sender, EventArgs e)
        {
            (sender as DispatcherTimer)?.Stop();
            _currentAffixPresetVisible = false;
        }

        private void DrawGraphics(object? sender, DrawGraphicsEventArgs e)
        {
            try
            {
                // Clear
                var gfx = e.Graphics;
                gfx.ClearScene();

                if (_window == null) return;

                // Tooltip
                lock (_lockItemTooltip)
                {
                    var overlayMenuItem = _overlayMenuItems.FirstOrDefault(o => o.Id.Equals("diablo"));
                    if (!overlayMenuItem?.IsLocked ?? true) _currentTooltip = new ItemTooltipDescriptor();

                    if (overlayMenuItem != null)
                    {
                        overlayMenuItem.Left = _window.Width * (_settingsManager.Settings.OverlayIconPosX / 1000f);
                        overlayMenuItem.Top = _window.Height * (_settingsManager.Settings.OverlayIconPosY / 1000f);
                    }

                    // Affixes
                    if (_currentTooltip.ItemAffixLocations.Any())
                    {
                        int length = 10;
                        int affixLocationHeight = 0;

                        for (int i = 0; i < _currentTooltip.ItemAffixLocations.Count; i++)
                        {
                            var itemAffixLocation = _currentTooltip.ItemAffixLocations[i];

                            float left = _currentTooltip.Location.X + _currentTooltip.Offset;
                            float top = _currentTooltip.Location.Y + itemAffixLocation.Y;

                            var itemAffix = _currentTooltip.ItemAffixes.FirstOrDefault(affix => affix.Item1 == i);
                            if (itemAffix != null)
                            {
                                gfx.OutlineFillCircle(_brushes[Colors.Black.ToString()], _brushes[itemAffix.Item2.Color.ToString()], left, top + (itemAffixLocation.Height / 2), length, 2);
                            }
                            else
                            {
                                if (_settingsManager.Settings.SelectedOverlayMarkerMode.Equals("Show All"))
                                {
                                    gfx.OutlineFillCircle(_brushes[Colors.Black.ToString()], _brushes[Colors.Red.ToString()], left, top + (itemAffixLocation.Height / 2), length, 2);
                                }
                            }
                        }
                    }

                    // Aspects
                    if (!_currentTooltip.ItemAspectLocation.IsEmpty)
                    {
                        int length = 10;

                        var itemAspectLocation = _currentTooltip.ItemAspectLocation;
                        float left = _currentTooltip.Location.X + _currentTooltip.Offset;
                        float top = _currentTooltip.Location.Y + itemAspectLocation.Y;

                        if (string.IsNullOrEmpty(_currentTooltip.ItemAspect.Id))
                        {
                            if (_settingsManager.Settings.SelectedOverlayMarkerMode.Equals("Show All"))
                            {
                                gfx.OutlineFillCircle(_brushes[Colors.Black.ToString()], _brushes[Colors.Red.ToString()], left, top + (itemAspectLocation.Height / 2), length, 2);
                            }
                        }
                        else
                        {
                            gfx.OutlineFillCircle(_brushes[Colors.Black.ToString()], _brushes[_currentTooltip.ItemAspect.Color.ToString()], left, top + (itemAspectLocation.Height / 2), length, 2);
                        }
                    }
                }

                // Menu items
                float stroke = 1; // Border arround menu items
                float captionOffset = 5; // Left margin for menu item caption
                float activationBarSize = 5; // Size for the activation bar
                foreach (OverlayMenuItem menuItem in _overlayMenuItems.OfType<OverlayMenuItem>())
                {
                    if (menuItem.IsVisible)
                    {
                        gfx.FillRectangle(_brushes["background"], menuItem.Left, menuItem.Top, menuItem.Left + menuItem.Width, menuItem.Top + menuItem.Height);
                        gfx.DrawRectangle(_brushes["border"], menuItem.Left, menuItem.Top, menuItem.Left + menuItem.Width, menuItem.Top + menuItem.Height, stroke);
                        gfx.DrawText(_fonts["consolasBold"], _brushes[menuItem.CaptionColor], menuItem.Left + captionOffset, menuItem.Top + menuItem.Height - activationBarSize - _fonts["consolasBold"].FontSize - captionOffset, menuItem.Caption);
                        gfx.DrawImage(_images[menuItem.Image], menuItem.Left + (menuItem.Width / 2) - (_images[menuItem.Image].Width / 2), menuItem.Top + (menuItem.Height / 3) - (_images[menuItem.Image].Height / 3));
                        float lockProgressAsWidth = (float)Math.Min(menuItem.LockWatch.ElapsedMilliseconds / OverlayConstants.LockTimer, 1.0) * menuItem.Width;
                        gfx.FillRectangle(_brushes[Colors.Goldenrod.ToString()], menuItem.Left, menuItem.Top + menuItem.Height - activationBarSize, menuItem.Left + lockProgressAsWidth, menuItem.Top + menuItem.Height);
                    }
                }

                // Affix preset name
                if (_currentAffixPresetVisible)
                {
                    float textOffset = 20;
                    float initialPresetPanelHeight = 50;
                    float fontSize = _settingsManager.Settings.OverlayFontSize;

                    // Limit preset text.
                    string presetText = _currentAffixPreset.Length <= 150 ? $"Preset \"{_currentAffixPreset}\" activated." :
                        $"Preset \"{_currentAffixPreset.Substring(0, 150)}\" activated.";

                    var textWidth = gfx.MeasureString(_fonts["consolasBold"], fontSize, presetText).X;
                    var textHeight = gfx.MeasureString(_fonts["consolasBold"], fontSize, presetText).Y;
                    float presetPanelWidth = textWidth + 2 * textOffset;

                    // Calculate the position of the panel to center it on the screen
                    float presetPanelLeft = (_window.Width - presetPanelWidth) / 2;
                    float presetPanelTop = (_window.Height - initialPresetPanelHeight) / 2;
                    float presetPanelWidthCentered = (_window.Width + presetPanelWidth) / 2;
                    float presetPanelHeightCentered = (_window.Height + initialPresetPanelHeight) / 2;

                    // Draw the panel as a filled rectangle behind the text
                    gfx.FillRectangle(_brushes["background"], presetPanelLeft, presetPanelTop, presetPanelWidthCentered, presetPanelHeightCentered);

                    // Draw the border of the panel
                    gfx.DrawRectangle(_brushes["border"], presetPanelLeft, presetPanelTop, presetPanelWidthCentered, presetPanelHeightCentered, stroke);

                    // Center the text inside the panel
                    float textLeft = presetPanelLeft + textOffset;
                    float textTop = presetPanelTop + (initialPresetPanelHeight - textHeight) / 2;
                    gfx.DrawText(_fonts["consolasBold"], fontSize, _brushes["text"], textLeft, textTop, presetText);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void DestroyGraphics(object? sender, DestroyGraphicsEventArgs e)
        {
            try
            {
                    foreach (var pair in _brushes) pair.Value.Dispose();
                    foreach (var pair in _fonts) pair.Value.Dispose();
                    foreach (var pair in _images) pair.Value.Dispose();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }
        private void SetupGraphics(object? sender, SetupGraphicsEventArgs e)
        {
            try
            {
                var gfx = e.Graphics;
                float fontSize = _settingsManager.Settings.OverlayFontSize;

                if (e.RecreateResources)
                {
                    foreach (var pair in _brushes) pair.Value.Dispose();
                    foreach (var pair in _images) pair.Value.Dispose();
                }

                var colorInfoList = GetColors();
                foreach (var colorInfo in colorInfoList) 
                {
                    _brushes[colorInfo.Value.ToString()] = gfx.CreateSolidBrush(colorInfo.Value.R, colorInfo.Value.G, colorInfo.Value.B);
                }
                _brushes["background"] = gfx.CreateSolidBrush(25, 25, 25);
                _brushes["border"] = gfx.CreateSolidBrush(75, 75, 75);
                _brushes["text"] = gfx.CreateSolidBrush(200, 200, 200);

                _images["diablo"] = gfx.CreateImage("./Images/Menu/icon_diablo.png");

                if (e.RecreateResources) return;

                //_fonts["arial"] = gfx.CreateFont("Arial", 12);
                //_fonts["consolas"] = gfx.CreateFont("Consolas", 14);
                _fonts["consolasBold"] = gfx.CreateFont("Consolas", fontSize, true);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void HandleAffixPresetChangedEvent(AffixPresetChangedEventParams affixPresetChangedEventParams)
        {
            _currentAffixPreset = affixPresetChangedEventParams.PresetName;
            _currentAffixPresetVisible = true;
            _currentAffixPresetTimer.Stop();
            _currentAffixPresetTimer.Start();
        }

        private void HandleMenuLockedEvent(MenuLockedEventParams menuLockedEventParams)
        {
            // Handle related actions
            HandleMenuItemAction(menuLockedEventParams.Id, true);
        }

        private void HandleMenuUnlockedEvent(MenuUnlockedEventParams menuUnlockedEventParams)
        {
            // Handle related actions
            HandleMenuItemAction(menuUnlockedEventParams.Id, false);
        }

        private void HandleToggleOverlayFromGUIEvent(ToggleOverlayFromGUIEventParams toggleOverlayFromGUIEventParams)
        {
            var overlayMenuItem = _overlayMenuItems.FirstOrDefault(o => o.Id.Equals("diablo"));
            if (overlayMenuItem != null)
            {
                overlayMenuItem.IsLocked = toggleOverlayFromGUIEventParams.IsEnabled;
            }
        }

        private void HandleTooltipDataReadyEvent(TooltipDataReadyEventParams tooltipDataReadyEventParams)
        {
            lock (_lockItemTooltip)
            {
                _currentTooltip = tooltipDataReadyEventParams.Tooltip;
            }
        }

        private void HandleWindowHandleUpdatedEvent(WindowHandleUpdatedEventParams windowHandleUpdatedEventParams)
        {
            // Check if the new WindowHandle is valid
            if (!IsValidWindowSize(windowHandleUpdatedEventParams.WindowHandle))
            {
                return;
            }

            // Check if the window bounds have changed
            if (HasNewWindowBounds(windowHandleUpdatedEventParams.WindowHandle)) 
            {
                _window?.FitTo(windowHandleUpdatedEventParams.WindowHandle);
                return;
            }

            // Check if there is a new windowhandle
            if (!_windowHandle.Equals(windowHandleUpdatedEventParams.WindowHandle))
            {
                _windowHandle = windowHandleUpdatedEventParams.WindowHandle;

                if (_window != null)
                {
                    _window?.FitTo(windowHandleUpdatedEventParams.WindowHandle);
                }
                else
                {
                    InitOverlayWindow();
                }
                return;
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitOverlayObjects()
        {
            _overlayMenuItems.Add(new OverlayMenuItem
            {
                Id = "diablo",
                Left = _settingsManager.Settings.OverlayIconPosX,
                Top = _settingsManager.Settings.OverlayIconPosY,
                Width = 50,
                Height = 50,
                Image = "diablo"
            });
        }

        private void InitOverlayWindow()
        {
            try
            {
                var gfx = new Graphics()
                {
                    MeasureFPS = true,
                    PerPrimitiveAntiAliasing = true,
                    TextAntiAliasing = true
                };

                _window = new GraphicsWindow(gfx)
                {
                    FPS = 60,
                    IsTopmost = true,
                    IsVisible = true
                };

                _window.DestroyGraphics += DestroyGraphics;
                _window.DrawGraphics += DrawGraphics;
                _window.SetupGraphics += SetupGraphics;

                Task.Run(() =>
                {
                    lock (_lockWindowHandle)
                    {
                        _window.Create();
                        _window.FitTo(_windowHandle);
                        _window.Join();
                    }
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void HandleMenuItemAction(string id, bool isLocked)
        {
            switch (id)
            {
                case "diablo":
                    if (isLocked)
                    {
                        // Turn overlay on
                        _eventAggregator.GetEvent<ToggleOverlayEvent>().Publish(new ToggleOverlayEventParams { IsEnabled = true });
                    }
                    else
                    {
                        // Turn overlay off
                        _eventAggregator.GetEvent<ToggleOverlayEvent>().Publish(new ToggleOverlayEventParams { IsEnabled = false });
                    }
                    break;
            }
        }

        private bool IsValidWindowSize(IntPtr windowHandle)
        {
            PInvoke.RECT rect;
            PInvoke.User32.GetWindowRect(windowHandle, out rect);

            //Debug.WriteLine($"Left: {rect.left}, Right: {rect.right}, Top: {rect.bottom}, Bottom: {rect.bottom}");

            var height = (rect.bottom - rect.top);

            return height > 100;
        }

        private bool HasNewWindowBounds(IntPtr windowHandle)
        {
            bool result = false;

            // Compare window bounds
            if (_window != null)
            {
                PInvoke.RECT rect;
                PInvoke.User32.GetWindowRect(windowHandle, out rect);

                result = _window.Height != (rect.bottom - rect.top) || _window.Width != (rect.right - rect.left) ||
                    rect.left != _window.X || rect.top != _window.Y;
            }

            return result;
        }

        private IEnumerable<KeyValuePair<string, System.Windows.Media.Color>> GetColors()
        {
            return typeof(Colors)
                .GetProperties()
                .Where(prop =>
                    typeof(System.Windows.Media.Color).IsAssignableFrom(prop.PropertyType))
                .Select(prop =>
                    new KeyValuePair<string, System.Windows.Media.Color>(prop.Name, (System.Windows.Media.Color)prop.GetValue(null)));
        }

        #endregion
    }

    public class OverlayMenuItem
    {
        protected readonly IEventAggregator _eventAggregator;

        // Start of Constructors region

        #region Constructors

        public OverlayMenuItem()
        {
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
            _eventAggregator.GetEvent<MouseUpdatedEvent>().Subscribe(HandleMouseUpdatedEvent);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public string Id { get; set; } = string.Empty;

        public float Left { get; set; }
        public float Top { get; set; }
        public float Height { get; set; }
        public float Width { get; set; }

        public string Caption { get; set; } = string.Empty;
        public string CaptionColor { get; set; } = "text";
        public string Image { get; set; } = string.Empty;
        public bool IsLocked { get; set; } = false;
        public bool IsVisible { get; set; } = false;
        public Stopwatch LockWatch { get; set; } = new Stopwatch();

        #endregion

        // Start of Event handlers region

        #region Event handlers

        protected void HandleMouseUpdatedEvent(MouseUpdatedEventParams mouseUpdatedEventParams)
        {
            bool isOnOverlayMenuItem = mouseUpdatedEventParams.CoordsMouseX >= Left && mouseUpdatedEventParams.CoordsMouseX <= Left + Width &&
                mouseUpdatedEventParams.CoordsMouseY >= Top && mouseUpdatedEventParams.CoordsMouseY <= Top + Height;

            if (isOnOverlayMenuItem)
            {
                if (!LockWatch.IsRunning && LockWatch.ElapsedMilliseconds == 0)
                {
                    // Start timer when mouse enters OverlayMenuItem
                    LockWatch.Start();
                }
                else
                {
                    if (LockWatch.IsRunning && LockWatch.ElapsedMilliseconds >= OverlayConstants.LockTimer)
                    {
                        // Stop timer and change lock state when mouse was on OverlayMenuItem for >= OverlayConstants.LockTimer ms
                        LockWatch.Stop();
                        IsLocked = !IsLocked;

                        // Let subscribers know when locked state has changed
                        if (IsLocked)
                        {
                            _eventAggregator.GetEvent<MenuLockedEvent>().Publish(new MenuLockedEventParams { Id = Id });
                        }
                        else
                        {
                            _eventAggregator.GetEvent<MenuUnlockedEvent>().Publish(new MenuUnlockedEventParams { Id = Id });
                        }
                    }
                }
            }
            else
            {
                // Stop and reset timer when mouse leaves OverlayMenuItem
                LockWatch.Reset();
            }

            // Update visibility
            if (IsLocked == true || isOnOverlayMenuItem)
            {
                IsVisible = true;
            }
            else if (IsLocked == false && isOnOverlayMenuItem == false)
            {
                IsVisible = false;
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
