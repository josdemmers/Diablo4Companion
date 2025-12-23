using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D4Companion.Entities;
using D4Companion.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class RenamePresetNameViewModel : ObservableObject
    {
        private readonly IAffixManager _affixManager;

        private string _name = string.Empty;

        // Start of Constructors region

        #region Constructors

        public RenamePresetNameViewModel(Action<RenamePresetNameViewModel?> closeHandler, StringWrapper presetName)
        {
            PresetName = presetName;
            Name = PresetName.String;

            // Init services
            _affixManager = App.Current.Services.GetRequiredService<IAffixManager>();

            // Init view commands
            CloseCommand = new RelayCommand<RenamePresetNameViewModel>(closeHandler);
            SetCancelCommand = new RelayCommand(SetCancelExecute);
            SetDoneCommand = new RelayCommand(SetDoneExecute, CanSetDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand SetCancelCommand { get; }
        public ICommand SetDoneCommand { get; }

        public bool IsCanceled { get; set; } = false;

        public string Name 
        {
            get => _name; 
            set
            {
                _name = value;
                PresetName.String = _name;
                OnPropertyChanged(nameof(Name));

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ((RelayCommand)SetDoneCommand)?.NotifyCanExecuteChanged();
                }); 
            }
        }

        public StringWrapper PresetName { get; set; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void SetCancelExecute()
        {
            IsCanceled = true;
            CloseCommand.Execute(this);
        }

        private bool CanSetDoneExecute()
        {
            return !_affixManager.AffixPresets.Any(p => p.Name.Equals(Name));
        }

        private void SetDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
