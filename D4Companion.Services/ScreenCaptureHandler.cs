using D4Companion.Constants;
using D4Companion.Events;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace D4Companion.Services
{
    public class ScreenCaptureHandler : IScreenCaptureHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private Bitmap? _currentScreen = null;
        private double _delayUpdateMouse = ScreenCaptureConstants.DelayMouse;
        private double _delayUpdateScreen = ScreenCaptureConstants.Delay;
        private ScreenCapture _screenCapture = new ScreenCapture();

        // Start of Constructors region

        #region Constructors

        public ScreenCaptureHandler(IEventAggregator eventAggregator, ILogger<ScreenCaptureHandler> logger, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);

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

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleApplicationLoadedEvent()
        {
            _ = StartMouseTask();
            _ = StartScreenTask();
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
                        _delayUpdateScreen = ScreenCaptureConstants.DelayError;
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
                        _delayUpdateMouse = ScreenCaptureConstants.DelayError;
                    }
                });
                await Task.Delay(TimeSpan.FromMilliseconds(_delayUpdateMouse));
            }
        }

        private void UpdateScreen()
        {
            IntPtr windowHandle = IntPtr.Zero;
            Process[] processes = new Process[0];

            if (_settingsManager.Settings.DebugMode)
            {
                // Debug mode - using firefox
                processes = Process.GetProcessesByName("firefox");
                foreach (Process p in processes)
                {
                    windowHandle = p.MainWindowHandle;
                    if (p.MainWindowTitle.StartsWith("Screenshot"))
                    {
                        break;
                    }
                }
            }
            else
            {
                // Release mode - using game client
                processes = Process.GetProcessesByName("Diablo IV");
                foreach (Process p in processes)
                {
                    windowHandle = p.MainWindowHandle;
                    if (p.MainWindowTitle.StartsWith("Diablo IV"))
                    {
                        break;
                    }
                }
            }

            if (windowHandle.ToInt64() > 0)
            {
                _eventAggregator.GetEvent<WindowHandleUpdatedEvent>().Publish(new WindowHandleUpdatedEventParams { WindowHandle = windowHandle });

                _currentScreen = _screenCapture.GetScreenCapture(windowHandle) ?? _currentScreen;
                //_currentScreen = new Bitmap("debug-path-to-image");

                _eventAggregator.GetEvent<ScreenCaptureReadyEvent>().Publish(new ScreenCaptureReadyEventParams
                {
                    CurrentScreen = _currentScreen
                });

                _delayUpdateScreen = ScreenCaptureConstants.Delay;

                //ScreenCapture.WriteBitmapToFile($"Logging/Screen_{DateTime.Now.ToFileTimeUtc()}.png", _currentScreen);
            }
            else
            {
                _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Invalid windowHandle. Diablo IV processes found: {processes.Length}. Retry in 10 seconds.");
                _delayUpdateScreen = ScreenCaptureConstants.DelayError; ;
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

            _eventAggregator.GetEvent<MouseUpdatedEvent>().Publish(new MouseUpdatedEventParams { CoordsMouseX = cursorInfo.ptScreenPos.x, CoordsMouseY = cursorInfo.ptScreenPos.y });

            _delayUpdateMouse = 100;
        }

        #endregion
    }
}
