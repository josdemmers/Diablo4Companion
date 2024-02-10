using D4Companion.Entities;
using D4Companion.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace D4Companion.ViewModels.Dialogs
{
    public class SetPresetNameViewModel : BindableBase
    {
        private StringWrapper _presetName = new();

        // Start of Constructors region

        #region Constructors

        public SetPresetNameViewModel(Action<SetPresetNameViewModel> closeHandler, StringWrapper presetName)
        {
            _presetName = presetName;

            // Init View commands
            CloseCommand = new DelegateCommand<SetPresetNameViewModel>(closeHandler);
            SetDoneCommand = new DelegateCommand(SetDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<SetPresetNameViewModel> CloseCommand { get; }
        public DelegateCommand SetDoneCommand { get; }

        public StringWrapper PresetName
        {
            get => _presetName;
            set
            {
                _presetName = value;
                RaisePropertyChanged(nameof(PresetName));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

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
