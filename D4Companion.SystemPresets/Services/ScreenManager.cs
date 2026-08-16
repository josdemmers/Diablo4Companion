using CommunityToolkit.Mvvm.Messaging;
using D4Companion.SystemPresets.Entities;
using D4Companion.SystemPresets.Interfaces;
using D4Companion.SystemPresets.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace D4Companion.SystemPresets.Services
{
    public class ScreenManager : IScreenManager
    {
        private readonly ILogger _logger;

        private double _delayUpdateScreen = 50;
        private List<MonitorDuplicator> _duplicators = [];
        private readonly List<ScreenCapture> _screenCaptures = [];        

        // Start of Constructors region

        #region Constructors

        public ScreenManager(ILogger<ScreenManager> logger)
        {
            // Init services
            _logger = logger;

            // Init messages
            WeakReferenceMessenger.Default.Register<ApplicationLoadedMessage>(this, HandleApplicationLoadedMessage);
            WeakReferenceMessenger.Default.Register<DuplicatorsCreatedMessage>(this, HandleDuplicatorsCreatedMessage);

        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<ScreenCapture> ScreenCaptures { get => _screenCaptures; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleApplicationLoadedMessage(object recipient, ApplicationLoadedMessage message)
        {
            _duplicators.Clear();
            _duplicators = MonitorCaptureFactory.CreateAllDuplicators();
            WeakReferenceMessenger.Default.Send(new DuplicatorsCreatedMessage());
        }

        private void HandleDuplicatorsCreatedMessage(object recipient, DuplicatorsCreatedMessage message)
        {
            _ = StartScreenTask();
        }

        #endregion

        // Start of Methods region

        #region Methods

        public void SaveBitmapSourceToFile(BitmapSource bitmap, string filePath)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        private async Task StartScreenTask()
        {
            while (true)
            {
                await Task.Run(() =>
                {
                    foreach (var duplicator in _duplicators)
                    {
                        var (bitmapSource, cursorX, cursorY) = duplicator.TryGetScreen();                        

                        if (bitmapSource != null)
                        {
                            bitmapSource.Freeze();

                            if (_screenCaptures.Any(s => s.DeviceName == duplicator.DeviceName))
                            {
                                _screenCaptures.First(s => s.DeviceName == duplicator.DeviceName).BitmapSource = bitmapSource;
                                _screenCaptures.First(s => s.DeviceName == duplicator.DeviceName).Timestamp = DateTime.Now;

                                WeakReferenceMessenger.Default.Send(new ScreenUpdatedMessage());

                                if (cursorX != 0 || cursorY != 0)
                                {
                                    WeakReferenceMessenger.Default.Send(new CursorUpdatedMessage(new CursorUpdatedMessageParams
                                    {
                                        X = cursorX,
                                        Y = cursorY
                                    }));
                                }                                
                            }
                            else
                            {
                                _screenCaptures.Add(new ScreenCapture
                                {
                                    BitmapSource = bitmapSource,
                                    DeviceName = duplicator.DeviceName
                                });

                                _screenCaptures.Sort((x, y) =>
                                {
                                    return string.Compare(x.DeviceName, y.DeviceName, StringComparison.Ordinal);
                                });

                                WeakReferenceMessenger.Default.Send(new ScreenAddedMessage());
                            }
                        }
                    }
                });
                await Task.Delay(TimeSpan.FromMilliseconds(_delayUpdateScreen));
            }
        }

        #endregion
    }
}
