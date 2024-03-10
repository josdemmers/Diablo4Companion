using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using Emgu.CV;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

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
            KeyBindingConfigSwitchPresetCommand = new DelegateCommand<object>(KeyBindingConfigSwitchPresetExecute);
            KeyBindingConfigToggleOverlayCommand = new DelegateCommand<object>(KeyBindingConfigToggleOverlayExecute);
            ToggleKeybindingOverlayCommand = new DelegateCommand(ToggleKeybindingOverlayExecute);
            ToggleKeybindingPresetsCommand = new DelegateCommand(ToggleKeybindingPresetsExecute);
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
        public DelegateCommand<object> KeyBindingConfigToggleOverlayCommand { get; }
        public DelegateCommand ToggleKeybindingPresetsCommand { get; set; }
        public DelegateCommand ToggleKeybindingOverlayCommand { get; set; }

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

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HotkeysConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private async void KeyBindingConfigToggleOverlayExecute(object obj)
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
            RaisePropertyChanged(nameof(KeyBindingConfigToggleOverlay));

            UpdateHotkeys();
        }

        private async void KeyBindingConfigSwitchPresetExecute(object obj)
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

            UpdateHotkeys();
        }

        private void ToggleKeybindingOverlayExecute()
        {
            _settingsManager.SaveSettings();
            UpdateHotkeys();
        }

        private void ToggleKeybindingPresetsExecute()
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
