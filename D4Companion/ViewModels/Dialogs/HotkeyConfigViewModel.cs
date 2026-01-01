using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D4Companion.Entities;
using D4Companion.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class HotkeyConfigViewModel : ObservableObject
    {
        private ObservableCollection<string> _keys = new ObservableCollection<string>();
        private ObservableCollection<string> _modifiers = new ObservableCollection<string>();

        private KeyBindingConfig _keyBindingConfig = new KeyBindingConfig();
        private string _selectedKey = string.Empty;
        private string _selectedModifier = string.Empty;

        // Start of Constructors region

        #region Constructors

        public HotkeyConfigViewModel(Action<HotkeyConfigViewModel?> closeHandler, KeyBindingConfig? keyBindingConfig)
        {
            // Init view commands
            CloseCommand = new RelayCommand<HotkeyConfigViewModel>(closeHandler);
            HotkeyConfigDoneCommand = new RelayCommand(HotkeyConfigDoneExecute);

            _keyBindingConfig = keyBindingConfig != null ? keyBindingConfig : throw new ArgumentNullException(nameof(keyBindingConfig));

            // Init Keys
            InitKeys();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand HotkeyConfigDoneCommand { get; }

        public ObservableCollection<string> Keys { get => _keys; set => _keys = value; }
        public ObservableCollection<string> Modifiers { get => _modifiers; set => _modifiers = value; }

        public KeyBindingConfig KeyBindingConfig { get => _keyBindingConfig; set => _keyBindingConfig = value; }
        public string SelectedKey
        {
            get => _selectedKey;
            set
            {
                if (_selectedKey != null && !string.IsNullOrWhiteSpace(value))
                {
                    _selectedKey = value;
                    KeyBindingConfig.KeyGestureKey = (Key)Enum.Parse(typeof(Key), value);
                    OnPropertyChanged(nameof(SelectedKey));
                }
            }
        }
        public string SelectedModifier
        {
            get => _selectedModifier;
            set
            {
                if (_selectedModifier != null && !string.IsNullOrWhiteSpace(value))
                {
                    _selectedModifier = value;
                    KeyBindingConfig.KeyGestureModifier = (ModifierKeys)Enum.Parse(typeof(ModifierKeys), value);
                    OnPropertyChanged(nameof(SelectedModifier));
                }
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HotkeyConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitKeys()
        {
            Keys.Clear();
            Keys.AddRange(Enum.GetNames(typeof(Key)));

            Modifiers.Clear();
            Modifiers.AddRange(Enum.GetNames(typeof(ModifierKeys)));

            SelectedKey = KeyBindingConfig.KeyGestureKey.ToString();
            SelectedModifier = KeyBindingConfig.KeyGestureModifier.ToString();
        }

        #endregion
    }
}
