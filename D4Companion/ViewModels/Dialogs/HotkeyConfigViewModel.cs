using D4Companion.Entities;
using Emgu.CV;
using Prism.Commands;
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
    public class HotkeyConfigViewModel : BindableBase
    {
        private ObservableCollection<string> _keys = new ObservableCollection<string>();
        private ObservableCollection<string> _modifiers = new ObservableCollection<string>();

        private KeyBindingConfig _keyBindingConfig = new KeyBindingConfig();
        private string _selectedKey = string.Empty;
        private string _selectedModifier = string.Empty;

        // Start of Constructors region

        #region Constructors

        public HotkeyConfigViewModel(Action<HotkeyConfigViewModel> closeHandler, KeyBindingConfig keyBindingConfig)
        {
            // Init View commands
            CloseCommand = new DelegateCommand<HotkeyConfigViewModel>(closeHandler);
            HotkeyConfigDoneCommand = new DelegateCommand(HotkeyConfigDoneExecute);

            _keyBindingConfig = keyBindingConfig;

            // Init Keys
            InitKeys();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<HotkeyConfigViewModel> CloseCommand { get; }
        public DelegateCommand HotkeyConfigDoneCommand { get; }

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
                    RaisePropertyChanged(nameof(SelectedKey));
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
                    RaisePropertyChanged(nameof(SelectedModifier));
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
