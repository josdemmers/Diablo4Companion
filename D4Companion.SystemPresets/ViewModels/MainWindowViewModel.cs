using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.SystemPresets.Interfaces;
using D4Companion.SystemPresets.Messages;
using D4Companion.SystemPresets.ViewModels.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace D4Companion.SystemPresets.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger _logger;
        private readonly IScreenManager _screenManager;

        private ObservableCollection<ScreenCaptureVM> _screenCaptures = [];

        private string _windowTitle = $"Diablo IV Companion - System Presets v{Assembly.GetExecutingAssembly().GetName().Version}";        

        // Start of Constructors region

        #region Constructors

        public MainWindowViewModel(ILogger<MainWindowViewModel> logger, IScreenManager screenManager)
        {
            // Init services
            _logger = logger;
            _screenManager = screenManager;

            // Init messages
            WeakReferenceMessenger.Default.Register<ActiveScreenChangedMessage>(this, HandleActiveScreenChangedMessage);
            WeakReferenceMessenger.Default.Register<ScreenAddedMessage>(this, HandleScreenAddedMessage);
            WeakReferenceMessenger.Default.Register<ScreenUpdatedMessage>(this, HandleScreenUpdatedMessage);            

            // Init view commands
            ApplicationLoadedCommand = new RelayCommand(ApplicationLoadedExecute);
        }        

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<ScreenCaptureVM> ScreenCaptures { get => _screenCaptures; set => _screenCaptures = value; }

        public ICommand ApplicationLoadedCommand { get; }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

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
        }

        #endregion

        // Start of Methods region

        #region Methods        

        #endregion
    }
}
