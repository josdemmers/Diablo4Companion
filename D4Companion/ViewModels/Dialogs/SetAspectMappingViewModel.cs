using D4Companion.Entities;
using D4Companion.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.ViewModels.Dialogs
{
    public class SetAspectMappingViewModel : BindableBase
    {
        private readonly ISystemPresetManager _systemPresetManager;

        private AspectInfo _aspectInfo = new AspectInfo();

        // Start of Constructors region

        #region Constructors

        public SetAspectMappingViewModel(Action<SetAspectMappingViewModel> closeHandler, AspectInfo aspectInfo)
        {
            // Init services
            _systemPresetManager = (ISystemPresetManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISystemPresetManager));

            // Init View commands
            CloseCommand = new DelegateCommand<SetAspectMappingViewModel>(closeHandler);
            SetDoneCommand = new DelegateCommand(SetDoneExecute);

            _aspectInfo = aspectInfo;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<SetAspectMappingViewModel> CloseCommand { get; }
        public DelegateCommand SetDoneCommand { get; }

        public AspectInfo AspectInfo
        {
            get => _aspectInfo;
            set
            {
                _aspectInfo = value;
                RaisePropertyChanged();
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
