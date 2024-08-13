using D4Companion.Constants;
using D4Companion.Events;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace D4Companion.Services
{
    public class ScreenCaptureHandler : IScreenCaptureHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private Bitmap? _currentScreen = null;
        private double _delayUpdateMouse = ScreenCaptureConstants.DelayMouse;
        private double _delayUpdateScreen = ScreenCaptureConstants.DefaultDelay;
        private bool _isEnabled = false;
        private bool _isSaveScreenshotRequested = false;
        private ScreenCapture _screenCapture = new ScreenCapture();
        private int _offsetTop = 0;
        private int _offsetLeft = 0;

        // Start of Constructors region

        #region Constructors

        public ScreenCaptureHandler(IEventAggregator eventAggregator, ILogger<ScreenCaptureHandler> logger, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);
            _eventAggregator.GetEvent<TakeScreenshotRequestedEvent>().Subscribe(HandleScreencaptureSaveRequestedEvent);
            _eventAggregator.GetEvent<ToggleDebugLockScreencaptureKeyBindingEvent>().Subscribe(HandleToggleDebugLockScreencaptureKeyBindingEvent);
            _eventAggregator.GetEvent<ToggleOverlayEvent>().Subscribe(HandleToggleOverlayEvent);
            _eventAggregator.GetEvent<ToggleOverlayFromGUIEvent>().Subscribe(HandleToggleOverlayFromGUIEvent);

            // Init logger
            _logger = logger;

            // Init services
            _settingsManager = settingsManager;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
        public bool IsScreencaptureLocked { get; private set; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleApplicationLoadedEvent()
        {
            _ = StartMouseTask();
            _ = StartScreenTask();
        }

        private void HandleScreencaptureSaveRequestedEvent()
        {
            _isSaveScreenshotRequested = true;
        }

        private void HandleToggleDebugLockScreencaptureKeyBindingEvent()
        {
            IsScreencaptureLocked = !IsScreencaptureLocked;
        }

        private void HandleToggleOverlayEvent(ToggleOverlayEventParams toggleOverlayEventParams)
        {
            IsEnabled = toggleOverlayEventParams.IsEnabled;
        }

        private void HandleToggleOverlayFromGUIEvent(ToggleOverlayFromGUIEventParams toggleOverlayFromGUIEventParams)
        {
            IsEnabled = toggleOverlayFromGUIEventParams.IsEnabled;
        }

        #endregion

        // Start of Methods region

        #region Methods

        private async Task StartScreenTask()
        {
            while (true)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        UpdateScreen();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
                        _delayUpdateScreen = ScreenCaptureConstants.DelayErrorShort;
                    }
                });
                await Task.Delay(TimeSpan.FromMilliseconds(_delayUpdateScreen));
            }
        }

        private async Task StartMouseTask()
        {
            while (true)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        //Application.Current.Dispatcher.Invoke((Action)delegate
                        //{
                            UpdateMouse();
                        //});

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
                        _delayUpdateMouse = ScreenCaptureConstants.DelayErrorShort;
                    }
                });
                await Task.Delay(TimeSpan.FromMilliseconds(_delayUpdateMouse));
            }
        }

        private void UpdateScreen()
        {
            // Note: Keep windowHandle local. On some systems making this a private class variable somehow locks the variable and prevents any garbage collection from happening.
            // Memory usage jumps to 10GB+ after a few minutes.
            IntPtr windowHandle = IntPtr.Zero;

            bool windowFound = false;
            Process[] processes = new Process[0];
            Process[] processesGeForceNOW = new Process[0];

            if (_settingsManager.Settings.DebugMode)
            {
                // Debug mode - using firefox
                processes = Process.GetProcessesByName("firefox");
                foreach (Process p in processes)
                {
                    windowHandle = p.MainWindowHandle;
                    if (p.MainWindowTitle.StartsWith("Screenshot"))
                    {
                        windowFound = true;
                        break;
                    }
                }
            }
            else
            {
                IntPtr windowHandleActive = PInvoke.User32.GetForegroundWindow();

                // Release mode - using game client
                processes = Process.GetProcessesByName("Diablo IV");
                foreach (Process p in processes)
                {
                    windowHandle = p.MainWindowHandle;
                    if (windowHandle == windowHandleActive)
                    {
                        windowFound = true;
                        break;
                    }
                }

                if (!windowFound)
                {
                    // Release mode - using GeForceNOW
                    processesGeForceNOW = Process.GetProcessesByName("GeForceNOW");
                    foreach (Process p in processesGeForceNOW)
                    {
                        windowHandle = p.MainWindowHandle;
                        if (windowHandle == windowHandleActive && p.MainWindowTitle.Contains("Diablo"))
                        {
                            windowFound = true;
                            break;
                        }
                    }
                }

                // Skip screencapture process when there is no active Diablo window.
                if (windowHandleActive == IntPtr.Zero || windowHandleActive != windowHandle)
                {
                    // Reset offset used for mousecoordinates.
                    _offsetTop = 0;
                    _offsetLeft = 0;

                    // Reset screencapture. Empty screencapture will clear tooltip.
                    _currentScreen = null;
                    _eventAggregator.GetEvent<ScreenCaptureReadyEvent>().Publish(new ScreenCaptureReadyEventParams
                    {
                        CurrentScreen = _currentScreen
                    });
                    _delayUpdateScreen = ScreenCaptureConstants.DelayErrorShort;
                    return;
                }
            }

            if (windowHandle.ToInt64() > 0)
            {
                _eventAggregator.GetEvent<WindowHandleUpdatedEvent>().Publish(new WindowHandleUpdatedEventParams { WindowHandle = windowHandle });

                // Update window position
                PInvoke.RECT region;
                PInvoke.User32.GetWindowRect(windowHandle, out region);
                _offsetTop = region.top;
                _offsetLeft = region.left;

                if (IsEnabled)
                {
                    if (!IsScreencaptureLocked)
                    {
                        _currentScreen = _screenCapture.GetScreenCapture(windowHandle) ?? _currentScreen;
                        //_currentScreen = new Bitmap("debug-path-to-image");
                    }

                    if (_isSaveScreenshotRequested)
                    {
                        _isSaveScreenshotRequested = false;
                        ScreenCapture.WriteBitmapToFile($"Screenshots/{_settingsManager.Settings.SelectedSystemPreset}_{DateTime.Now.ToFileTimeUtc()}.png", _currentScreen);
                    }

                    _eventAggregator.GetEvent<ScreenCaptureReadyEvent>().Publish(new ScreenCaptureReadyEventParams
                    {
                        CurrentScreen = _currentScreen
                    });
                }

                _delayUpdateScreen = _settingsManager.Settings.ScreenCaptureDelay;
            }
            else
            {
                _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Invalid windowHandle. Diablo IV processes found: {processes.Length}. Retry in {ScreenCaptureConstants.DelayError / 1000} seconds.");
                _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Invalid windowHandle. Diablo IV (GeForceNOW) processes found: {processesGeForceNOW.Length}. Retry in {ScreenCaptureConstants.DelayError / 1000} seconds.");
                _delayUpdateScreen = ScreenCaptureConstants.DelayError;
            }
        }

        private void UpdateMouse()
        {
            PInvoke.User32.CURSORINFO cursorInfo = new PInvoke.User32.CURSORINFO();
            cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
            PInvoke.User32.GetCursorInfo(ref cursorInfo);

            //var monitor = PInvoke.User32.MonitorFromPoint(cursorInfo.ptScreenPos, PInvoke.User32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
            //var dpi = PInvoke.User32.GetDpiForMonitor(monitor, PInvoke.User32.MonitorDpiType.EFFECTIVE_DPI, out int dpiX, out int dpiY);
            var dpi = PInvoke.User32.GetDpiForSystem();
            var dpiScaling = Math.Round(dpi / (double)96, 2);

            string mouseCoordinates = $"X: {cursorInfo.ptScreenPos.x}, Y: {cursorInfo.ptScreenPos.y}";
            string mouseCoordinatesScaled = $"X: {(int)(cursorInfo.ptScreenPos.x / dpiScaling)}, Y: {(int)(cursorInfo.ptScreenPos.y / dpiScaling)}";
            string mouseCoordinatesWindow = $"X: {cursorInfo.ptScreenPos.x - _offsetLeft}, Y: {cursorInfo.ptScreenPos.y - _offsetTop}";
            string mouseCoordinatesWindowScaled = $"X: {(int)((cursorInfo.ptScreenPos.x - _offsetLeft) / dpiScaling)}, Y: {(int)((cursorInfo.ptScreenPos.y - _offsetTop) / dpiScaling)}";

            _eventAggregator.GetEvent<MouseUpdatedEvent>().Publish(new MouseUpdatedEventParams { CoordsMouseX = cursorInfo.ptScreenPos.x - _offsetLeft, CoordsMouseY = cursorInfo.ptScreenPos.y - _offsetTop });

            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {mouseCoordinates}");
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {mouseCoordinatesScaled} (SCALED)");
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {mouseCoordinatesWindow}");
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {mouseCoordinatesWindowScaled} (SCALED)");

            _delayUpdateMouse = 100;
        }

        #endregion
    }
}
