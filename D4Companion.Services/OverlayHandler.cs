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

namespace D4Companion.Services
{
    public class OverlayHandler : IOverlayHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private GraphicsWindow? _window = null;
        private Graphics? _gfx = null;
        private readonly Dictionary<string, SolidBrush> _brushes = new Dictionary<string, SolidBrush>();
        private readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();
        private readonly Dictionary<string, Image> _images = new Dictionary<string, Image>();

        private ItemTooltipDescriptor _currentTooltip = new ItemTooltipDescriptor();
        private object _lockItemTooltip = new object();
        private List<OverlayMenuItem> _overlayMenuItems = new List<OverlayMenuItem>();
        IntPtr _windowHandle = IntPtr.Zero;

        // Start of Constructors region

        #region Constructors

        public OverlayHandler(IEventAggregator eventAggregator, ILogger<ScreenProcessHandler> logger, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
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

        private void DrawGraphics(object? sender, DrawGraphicsEventArgs e)
        {
            try
            {
                // Clear
                var gfx = e.Graphics;
                gfx.ClearScene();

                // Tooltip
                lock (_lockItemTooltip)
                {
                    var overlayMenuItem = _overlayMenuItems.FirstOrDefault(o => o.Id.Equals("diablo"));
                    if (!overlayMenuItem?.IsLocked ?? true) _currentTooltip = new ItemTooltipDescriptor();

                    // Affixes
                    if (_currentTooltip.ItemAffixLocations.Any())
                    {
                        int length = 10;
                        int affixLocationHeight = 0;

                        foreach (var itemAffixLocation in _currentTooltip.ItemAffixLocations)
                        {
                            float left = _currentTooltip.Location.X;
                            float top = _currentTooltip.Location.Y + itemAffixLocation.Y;
                            affixLocationHeight = itemAffixLocation.Height;

                            if (!CheckAffixLocationHasPreferedAffix(_currentTooltip, top + (itemAffixLocation.Height / 2)))
                            {
                                gfx.OutlineFillCircle(_brushes["black"], _brushes["red"], left, top + (itemAffixLocation.Height / 2), length, 2);

                                // Note: Inverse logic for selected sigil affixes
                                //if (_currentTooltip.ItemType.ToLower().Contains("sigil_"))
                                //    gfx.OutlineFillCircle(_brushes["black"], _brushes["green"], left, top + (itemAffixLocation.Height / 2), length, 2);
                            }
                            else
                            {
                                gfx.OutlineFillCircle(_brushes["black"], _brushes["green"], left, top + (itemAffixLocation.Height / 2), length, 2);

                                // Note: Inverse logic for selected sigil affixes
                                //if (_currentTooltip.ItemType.ToLower().Contains("sigil_"))
                                //    gfx.OutlineFillCircle(_brushes["black"], _brushes["red"], left, top + (itemAffixLocation.Height / 2), length, 2);
                            }
                        }
                    }

                    // Aspects
                    if (!_currentTooltip.ItemAspectLocation.IsEmpty)
                    {
                        int length = 10;

                        var itemAspectLocation = _currentTooltip.ItemAspectLocation;
                        float left = _currentTooltip.Location.X;
                        float top = _currentTooltip.Location.Y + itemAspectLocation.Y;

                        if (_currentTooltip.ItemAspect.IsEmpty)
                        {
                            gfx.OutlineFillCircle(_brushes["black"], _brushes["red"], left, top + (itemAspectLocation.Height / 2), length, 2);
                        }
                        else
                        {
                            gfx.OutlineFillCircle(_brushes["black"], _brushes["green"], left, top + (itemAspectLocation.Height / 2), length, 2);
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
                        gfx.FillRectangle(_brushes["darkyellow"], menuItem.Left, menuItem.Top + menuItem.Height - activationBarSize, menuItem.Left + lockProgressAsWidth, menuItem.Top + menuItem.Height);
                    }
                }
            }
            catch(Exception exception)
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

                if (e.RecreateResources)
                {
                    foreach (var pair in _brushes) pair.Value.Dispose();
                    foreach (var pair in _images) pair.Value.Dispose();
                }

                _brushes["black"] = gfx.CreateSolidBrush(0, 0, 0);
                _brushes["white"] = gfx.CreateSolidBrush(255, 255, 255);
                _brushes["red"] = gfx.CreateSolidBrush(255, 0, 0);
                _brushes["red200"] = gfx.CreateSolidBrush(200, 0, 0);
                _brushes["green"] = gfx.CreateSolidBrush(0, 255, 0);
                _brushes["blue"] = gfx.CreateSolidBrush(0, 0, 255);
                _brushes["darkyellow"] = gfx.CreateSolidBrush(255, 204, 0);
                _brushes["background"] = gfx.CreateSolidBrush(25, 25, 25);
                _brushes["border"] = gfx.CreateSolidBrush(75, 75, 75);
                _brushes["text"] = gfx.CreateSolidBrush(200, 200, 200);

                _images["diablo"] = gfx.CreateImage("./Images/Menu/icon_diablo.png");

                if (e.RecreateResources) return;

                _fonts["arial"] = gfx.CreateFont("Arial", 12);
                _fonts["consolas"] = gfx.CreateFont("Consolas", 14);
                _fonts["consolasBold"] = gfx.CreateFont("Consolas", 18, true);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
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
            if (!_windowHandle.Equals(windowHandleUpdatedEventParams.WindowHandle))
            {
                if(_window != null)
                {
                    _window.DestroyGraphics -= DestroyGraphics;
                    _window.DrawGraphics -= DrawGraphics;
                    _window.SetupGraphics -= SetupGraphics;
                    _window.Dispose();
                }
                _windowHandle = windowHandleUpdatedEventParams.WindowHandle;
                InitOverlayWindow();
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
                Left = 10,
                Top = 10,
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

                _gfx = gfx;

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
                    _window.Create();
                    _window.FitTo(_windowHandle);
                    _window.Join();
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

        private bool CheckAffixLocationHasPreferedAffix(ItemTooltipDescriptor tooltip, float top)
        {
            foreach (var itemAffix in tooltip.ItemAffixes)
            {
                float affixTop = tooltip.Location.Y + itemAffix.Top;
                float affixBottom = tooltip.Location.Y + itemAffix.Bottom;

                if (top >= affixTop && top <= affixBottom) return true;
            }

            return false;
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
