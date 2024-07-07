using D4Companion.Entities;
using D4Companion.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Linq;

namespace D4Companion.ViewModels.Dialogs
{
    public class SetPresetNameViewModel : BindableBase
    {
        private readonly IAffixManager _affixManager;

        private string _name = string.Empty;

        // Start of Constructors region

        #region Constructors

        public SetPresetNameViewModel(Action<SetPresetNameViewModel> closeHandler, StringWrapper presetName)
        {
            PresetName = presetName;
            Name = PresetName.String;

            // Init services
            _affixManager = (IAffixManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAffixManager));

            // Init View commands
            CloseCommand = new DelegateCommand<SetPresetNameViewModel>(closeHandler);
            SetCancelCommand = new DelegateCommand(SetCancelExecute);
            SetDoneCommand = new DelegateCommand(SetDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<SetPresetNameViewModel> CloseCommand { get; }
        public DelegateCommand SetCancelCommand { get; }
        public DelegateCommand SetDoneCommand { get; }

        public bool IsCanceled { get; set; } = false;

        public bool ShowOverwriteWarning
        {
            get
            {
                return _affixManager.AffixPresets.Any(p => p.Name.Equals(Name));
            }
        }

        public string Name 
        {
            get => _name; 
            set
            {
                _name = value;
                PresetName.String = _name;
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(ShowOverwriteWarning));
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
