using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;

namespace D4Companion.ViewModels.Dialogs
{
    public class HotkeysConfigViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        private ObservableCollection<string> _keys = new ObservableCollection<string>();
        private ObservableCollection<string> _modifiers = new ObservableCollection<string>();

        private string _selectedKey = string.Empty;
        private string _selectedModifier = string.Empty;

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
            KeyBindingConfigTakeScreenshotCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigToggleOverlayCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigAspectCounterIncreaseCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigAspectCounterDecreaseCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigAspectCounterResetCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            KeyBindingConfigToggleDebugLockScreencaptureCommand = new DelegateCommand<object>(KeyBindingConfigExecute);
            ToggleKeybindingOverlayCommand = new DelegateCommand(ToggleKeybindingExecute);
            ToggleKeybindingPresetsCommand = new DelegateCommand(ToggleKeybindingExecute);
            ToggleKeybindingTakeScreenshotCommand = new DelegateCommand(ToggleKeybindingExecute);
            ToggleKeybindingAspectCounterIncreaseCommand = new DelegateCommand(ToggleKeybindingExecute);
            ToggleKeybindingAspectCounterDecreaseCommand = new DelegateCommand(ToggleKeybindingExecute);
            ToggleKeybindingAspectCounterResetCommand = new DelegateCommand(ToggleKeybindingExecute);
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
        public DelegateCommand<object> KeyBindingConfigTakeScreenshotCommand { get; }
        public DelegateCommand<object> KeyBindingConfigToggleOverlayCommand { get; }
        public DelegateCommand<object> KeyBindingConfigAspectCounterIncreaseCommand { get; }
        public DelegateCommand<object> KeyBindingConfigAspectCounterDecreaseCommand { get; }
        public DelegateCommand<object> KeyBindingConfigAspectCounterResetCommand { get; }
        public DelegateCommand<object> KeyBindingConfigToggleDebugLockScreencaptureCommand { get; }
        public DelegateCommand ToggleKeybindingOverlayCommand { get; set; }
        public DelegateCommand ToggleKeybindingPresetsCommand { get; set; }
        public DelegateCommand ToggleKeybindingTakeScreenshotCommand { get; set; }
        public DelegateCommand ToggleKeybindingAspectCounterIncreaseCommand { get; set; }
        public DelegateCommand ToggleKeybindingAspectCounterDecreaseCommand { get; set; }
        public DelegateCommand ToggleKeybindingAspectCounterResetCommand { get; set; }
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

        public KeyBindingConfig KeyBindingConfigAspectCounterIncrease
        {
            get => _settingsManager.Settings.KeyBindingConfigAspectCounterIncrease;
            set
            {
                if (value != null)
                {
                    _settingsManager.Settings.KeyBindingConfigAspectCounterIncrease = value;
                    RaisePropertyChanged(nameof(KeyBindingConfigAspectCounterIncrease));

                    _settingsManager.SaveSettings();
                }
            }
        }

        public KeyBindingConfig KeyBindingConfigAspectCounterDecrease
        {
            get => _settingsManager.Settings.KeyBindingConfigAspectCounterDecrease;
            set
            {
                if (value != null)
                {
                    _settingsManager.Settings.KeyBindingConfigAspectCounterDecrease = value;
                    RaisePropertyChanged(nameof(KeyBindingConfigAspectCounterDecrease));

                    _settingsManager.SaveSettings();
                }
            }
        }

        public KeyBindingConfig KeyBindingConfigAspectCounterReset
        {
            get => _settingsManager.Settings.KeyBindingConfigAspectCounterReset;
            set
            {
                if (value != null)
                {
                    _settingsManager.Settings.KeyBindingConfigAspectCounterReset = value;
                    RaisePropertyChanged(nameof(KeyBindingConfigAspectCounterReset));

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
            RaisePropertyChanged(nameof(KeyBindingConfigTakeScreenshot));
            RaisePropertyChanged(nameof(KeyBindingConfigToggleOverlay));
            RaisePropertyChanged(nameof(KeyBindingConfigAspectCounterIncrease));
            RaisePropertyChanged(nameof(KeyBindingConfigAspectCounterDecrease));
            RaisePropertyChanged(nameof(KeyBindingConfigAspectCounterReset));
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
