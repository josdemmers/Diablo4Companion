using D4Companion.Interfaces;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;

namespace D4Companion.ViewModels.Dialogs
{
    public class ParagonConfigViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public ParagonConfigViewModel(Action<ParagonConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            CloseCommand = new DelegateCommand<ParagonConfigViewModel>(closeHandler);
            ParagonConfigDoneCommand = new DelegateCommand(ParagonConfigDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<ParagonConfigViewModel> CloseCommand { get; }
        public DelegateCommand ParagonConfigDoneCommand { get; }

        public int ParagonNodeSize
        {
            get => _settingsManager.Settings.ParagonNodeSize;
            set
            {
                _settingsManager.Settings.ParagonNodeSize = value;
                RaisePropertyChanged(nameof(ParagonNodeSize));

                _settingsManager.SaveSettings();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void ParagonConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
