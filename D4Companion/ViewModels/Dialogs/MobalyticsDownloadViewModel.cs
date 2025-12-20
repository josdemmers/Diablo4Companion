using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.ViewModels.Entities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using SharpDX.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace D4Companion.ViewModels.Dialogs
{
    public class MobalyticsDownloadViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;

        private ObservableCollection<string> _variants = new ObservableCollection<string>();

        private string _buildName = string.Empty;
        private bool _mobalyticsCompleted = false;
        private string _status = "Preparing browser instance.";

        // Start of Constructors region

        #region Constructors

        public MobalyticsDownloadViewModel(Action<MobalyticsDownloadViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
            _eventAggregator.GetEvent<MobalyticsCompletedEvent>().Subscribe(HandleMobalyticsCompletedEvent);
            _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Subscribe(HandleMobalyticsStatusUpdateEvent);

            // Init View commands
            CloseCommand = new DelegateCommand<MobalyticsDownloadViewModel>(closeHandler);
            SetDoneCommand = new DelegateCommand(SetDoneExecute, CanSetDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<MobalyticsDownloadViewModel> CloseCommand { get; }
        public DelegateCommand SetDoneCommand { get; }

        public ObservableCollection<string> Variants { get => _variants; set => _variants = value; }

        public string BuildName
        {
            get => _buildName;
            set
            {
                _buildName = value;
                RaisePropertyChanged(nameof(BuildName));
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                RaisePropertyChanged(nameof(Status));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleMobalyticsCompletedEvent()
        {
            _mobalyticsCompleted = true;
            SetDoneCommand.RaiseCanExecuteChanged();
            CloseCommand.Execute(this);
        }

        private void HandleMobalyticsStatusUpdateEvent(MobalyticsStatusUpdateEventParams mobalyticsStatusUpdateEventParams)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Variants.Clear();

                BuildName = mobalyticsStatusUpdateEventParams.Build.Name;
                Status = $"Status: {mobalyticsStatusUpdateEventParams.Status}";
                Variants.AddRange(mobalyticsStatusUpdateEventParams.Build.Variants.Select(v => v.Name));
            });
        }

        private bool CanSetDoneExecute()
        {
            return _mobalyticsCompleted;
        }

        private void SetDoneExecute()
        {
            _eventAggregator.GetEvent<MobalyticsCompletedEvent>().Unsubscribe(HandleMobalyticsCompletedEvent);
            _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Unsubscribe(HandleMobalyticsStatusUpdateEvent);

            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
