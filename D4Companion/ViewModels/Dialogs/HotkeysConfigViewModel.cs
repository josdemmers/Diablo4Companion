using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;

namespace D4Companion.ViewModels.Dialogs
{
    public class HotkeysConfigViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public HotkeysConfigViewModel(Action<HotkeysConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            CloseCommand = new DelegateCommand<HotkeysConfigViewModel>(closeHandler);
            HotkeysConfigDoneCommand = new DelegateCommand(HotkeysConfigDoneExecute);
            KeyBindingConfigSwitchPresetCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigSwitchOverlayCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigTakeScreenshotCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigToggleControllerCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigToggleOverlayCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigToggleDebugLockScreencaptureCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            ToggleKeybindingControllerCommand = new DelegateCommand(ToggleKeybindingExecute);
            ToggleKeybindingOverlayCommand = new DelegateCommand(ToggleKeybindingExecute);
            ToggleKeybindingSwitchPresetsCommand = new DelegateCommand(ToggleKeybindingExecute);
            ToggleKeybindingSwitchOverlayCommand = new DelegateCommand(ToggleKeybindingExecute);
            ToggleKeybindingTakeScreenshotCommand = new DelegateCommand(ToggleKeybindingExecute);
            ToggleKeybindingDebugLockScreencaptureCommand = new DelegateCommand(ToggleKeybindingExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<HotkeysConfigViewModel> CloseCommand { get; }
        public DelegateCommand HotkeysConfigDoneCommand { get; }
        public DelegateCommand<object> KeyBindingConfigSwitchPresetCommand { get; }
        public DelegateCommand<object> KeyBindingConfigSwitchOverlayCommand { get; }
        public DelegateCommand<object> KeyBindingConfigTakeScreenshotCommand { get; }
        public DelegateCommand<object> KeyBindingConfigToggleControllerCommand { get; }
        public DelegateCommand<object> KeyBindingConfigToggleOverlayCommand { get; }
        public DelegateCommand<object> KeyBindingConfigToggleDebugLockScreencaptureCommand { get; }
        public DelegateCommand ToggleKeybindingControllerCommand { get; set; }
        public DelegateCommand ToggleKeybindingOverlayCommand { get; set; }
        public DelegateCommand ToggleKeybindingSwitchPresetsCommand { get; set; }
        public DelegateCommand ToggleKeybindingSwitchOverlayCommand { get; set; }
        public DelegateCommand ToggleKeybindingTakeScreenshotCommand { get; set; }
        public DelegateCommand ToggleKeybindingDebugLockScreencaptureCommand { get; set; }

        public KeyBindingConfig KeyBindingConfigSwitchPreset
        {
            get => _settingsManager.Settings.KeyBindingConfigSwitchPreset;
            set
            {
                if (value != null)
                {
                    _settingsManager.Settings.KeyBindingConfigSwitchPreset = value;
                    RaisePropertyChanged(nameof(KeyBindingConfigSwitchPreset));

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
                    RaisePropertyChanged(nameof(KeyBindingConfigSwitchOverlay));

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
                    RaisePropertyChanged(nameof(KeyBindingConfigTakeScreenshot));

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
                    RaisePropertyChanged(nameof(KeyBindingConfigToggleController));

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
                    RaisePropertyChanged(nameof(KeyBindingConfigToggleOverlay));

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
                    RaisePropertyChanged(nameof(KeyBindingConfigToggleDebugLockScreencapture));

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

        private async void KeyBindingConfigExecute(object obj)
        {
            var hotkeyConfigDialog = new CustomDialog() { Title = "Hotkey config" };
            var dataContext = new HotkeyConfigViewModel(async instance =>
            {
                await hotkeyConfigDialog.WaitUntilUnloadedAsync();
            }, (KeyBindingConfig)obj);
            hotkeyConfigDialog.Content = new HotkeyConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, hotkeyConfigDialog);
            await hotkeyConfigDialog.WaitUntilUnloadedAsync();

            _settingsManager.SaveSettings();
            RaisePropertyChanged(nameof(KeyBindingConfigSwitchPreset));
            RaisePropertyChanged(nameof(KeyBindingConfigSwitchOverlay));
            RaisePropertyChanged(nameof(KeyBindingConfigTakeScreenshot));
            RaisePropertyChanged(nameof(KeyBindingConfigToggleController));
            RaisePropertyChanged(nameof(KeyBindingConfigToggleOverlay));
            RaisePropertyChanged(nameof(KeyBindingConfigToggleDebugLockScreencapture));

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
            _eventAggregator.GetEvent<UpdateHotkeysRequestEvent>().Publish();
        }

        #endregion
    }
}
