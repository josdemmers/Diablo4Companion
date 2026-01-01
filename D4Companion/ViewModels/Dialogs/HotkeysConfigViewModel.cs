using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Interfaces;
using D4Companion.Messages;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class HotkeysConfigViewModel : ObservableObject
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public HotkeysConfigViewModel(Action<HotkeysConfigViewModel?> closeHandler)
        {
            // Init services
            _dialogCoordinator = App.Current.Services.GetRequiredService<IDialogCoordinator>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init view commands
            CloseCommand = new RelayCommand<HotkeysConfigViewModel>(closeHandler);
            HotkeysConfigDoneCommand = new RelayCommand(HotkeysConfigDoneExecute);
            KeyBindingConfigSwitchPresetCommand = new RelayCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigSwitchOverlayCommand = new RelayCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigTakeScreenshotCommand = new RelayCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigToggleControllerCommand = new RelayCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigToggleOverlayCommand = new RelayCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigToggleDebugLockScreencaptureCommand = new RelayCommand<object>(KeyBindingConfigExecute);
            ToggleKeybindingControllerCommand = new RelayCommand(ToggleKeybindingExecute);
            ToggleKeybindingOverlayCommand = new RelayCommand(ToggleKeybindingExecute);
            ToggleKeybindingSwitchPresetsCommand = new RelayCommand(ToggleKeybindingExecute);
            ToggleKeybindingSwitchOverlayCommand = new RelayCommand(ToggleKeybindingExecute);
            ToggleKeybindingTakeScreenshotCommand = new RelayCommand(ToggleKeybindingExecute);
            ToggleKeybindingDebugLockScreencaptureCommand = new RelayCommand(ToggleKeybindingExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand HotkeysConfigDoneCommand { get; }
        public ICommand KeyBindingConfigSwitchPresetCommand { get; }
        public ICommand KeyBindingConfigSwitchOverlayCommand { get; }
        public ICommand KeyBindingConfigTakeScreenshotCommand { get; }
        public ICommand KeyBindingConfigToggleControllerCommand { get; }
        public ICommand KeyBindingConfigToggleOverlayCommand { get; }
        public ICommand KeyBindingConfigToggleDebugLockScreencaptureCommand { get; }
        public ICommand ToggleKeybindingControllerCommand { get; set; }
        public ICommand ToggleKeybindingOverlayCommand { get; set; }
        public ICommand ToggleKeybindingSwitchPresetsCommand { get; set; }
        public ICommand ToggleKeybindingSwitchOverlayCommand { get; set; }
        public ICommand ToggleKeybindingTakeScreenshotCommand { get; set; }
        public ICommand ToggleKeybindingDebugLockScreencaptureCommand { get; set; }

        public KeyBindingConfig KeyBindingConfigSwitchPreset
        {
            get => _settingsManager.Settings.KeyBindingConfigSwitchPreset;
            set
            {
                if (value != null)
                {
                    _settingsManager.Settings.KeyBindingConfigSwitchPreset = value;
                    OnPropertyChanged(nameof(KeyBindingConfigSwitchPreset));

                    _settingsManager.SaveSettings();
                }
            }
        }

        public KeyBindingConfig KeyBindingConfigSwitchOverlay
        {
            get => _settingsManager.Settings.KeyBindingConfigSwitchOverlay;
            set
            {
                if (value != null)
                {
                    _settingsManager.Settings.KeyBindingConfigSwitchOverlay = value;
                    OnPropertyChanged(nameof(KeyBindingConfigSwitchOverlay));

                    _settingsManager.SaveSettings();
                }
            }
        }

        public KeyBindingConfig KeyBindingConfigTakeScreenshot
        {
            get => _settingsManager.Settings.KeyBindingConfigTakeScreenshot;
            set
            {
                if (value != null)
                {
                    _settingsManager.Settings.KeyBindingConfigTakeScreenshot = value;
                    OnPropertyChanged(nameof(KeyBindingConfigTakeScreenshot));

                    _settingsManager.SaveSettings();
                }
            }
        }

        public KeyBindingConfig KeyBindingConfigToggleController
        {
            get => _settingsManager.Settings.KeyBindingConfigToggleController;
            set
            {
                if (value != null)
                {
                    _settingsManager.Settings.KeyBindingConfigToggleController = value;
                    OnPropertyChanged(nameof(KeyBindingConfigToggleController));

                    _settingsManager.SaveSettings();
                }
            }
        }

        public KeyBindingConfig KeyBindingConfigToggleOverlay
        {
            get => _settingsManager.Settings.KeyBindingConfigToggleOverlay;
            set
            {
                if (value != null)
                {
                    _settingsManager.Settings.KeyBindingConfigToggleOverlay = value;
                    OnPropertyChanged(nameof(KeyBindingConfigToggleOverlay));

                    _settingsManager.SaveSettings();
                }
            }
        }

        public KeyBindingConfig KeyBindingConfigToggleDebugLockScreencapture
        {
            get => _settingsManager.Settings.KeyBindingConfigToggleDebugLockScreencapture;
            set
            {
                if (value != null)
                {
                    _settingsManager.Settings.KeyBindingConfigToggleDebugLockScreencapture = value;
                    OnPropertyChanged(nameof(KeyBindingConfigToggleDebugLockScreencapture));

                    _settingsManager.SaveSettings();
                }
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HotkeysConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private async void KeyBindingConfigExecute(object? obj)
        {
            var hotkeyConfigDialog = new CustomDialog() { Title = "Hotkey config" };
            var dataContext = new HotkeyConfigViewModel(async instance =>
            {
                await hotkeyConfigDialog.WaitUntilUnloadedAsync();
            }, (KeyBindingConfig?)obj);
            hotkeyConfigDialog.Content = new HotkeyConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, hotkeyConfigDialog);
            await hotkeyConfigDialog.WaitUntilUnloadedAsync();

            _settingsManager.SaveSettings();
            OnPropertyChanged(nameof(KeyBindingConfigSwitchPreset));
            OnPropertyChanged(nameof(KeyBindingConfigSwitchOverlay));
            OnPropertyChanged(nameof(KeyBindingConfigTakeScreenshot));
            OnPropertyChanged(nameof(KeyBindingConfigToggleController));
            OnPropertyChanged(nameof(KeyBindingConfigToggleOverlay));
            OnPropertyChanged(nameof(KeyBindingConfigToggleDebugLockScreencapture));

            UpdateHotkeys();
        }


        private void ToggleKeybindingExecute()
        {
            _settingsManager.SaveSettings();
            UpdateHotkeys();
        }


        #endregion

        // Start of Methods region

        #region Methods

        private void UpdateHotkeys()
        {
            WeakReferenceMessenger.Default.Send(new UpdateHotkeysRequestMessage());
        }

        #endregion
    }
}
