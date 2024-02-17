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
    public class D4BuildsDownloadViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;

        private ObservableCollection<string> _variants = new ObservableCollection<string>();

        private string _buildName = string.Empty;
        private bool _d4BuildsComplated = false;
        private string _status = string.Empty;

        // Start of Constructors region

        #region Constructors

        public D4BuildsDownloadViewModel(Action<D4BuildsDownloadViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
            _eventAggregator.GetEvent<D4BuildsCompletedEvent>().Subscribe(HandleD4BuildsCompletedEvent);
            _eventAggregator.GetEvent<D4BuildsStatusUpdateEvent>().Subscribe(HandleD4BuildsStatusUpdateEvent);

            // Init View commands
            CloseCommand = new DelegateCommand<D4BuildsDownloadViewModel>(closeHandler);
            SetDoneCommand = new DelegateCommand(SetDoneExecute, CanSetDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<D4BuildsDownloadViewModel> CloseCommand { get; }
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

        private void HandleD4BuildsCompletedEvent()
        {
            _d4BuildsComplated = true;
            SetDoneCommand.RaiseCanExecuteChanged();
            CloseCommand.Execute(this);
        }

        private void HandleD4BuildsStatusUpdateEvent(D4BuildsStatusUpdateEventParams d4BuildsStatusUpdateEventParams)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Variants.Clear();

                BuildName = d4BuildsStatusUpdateEventParams.Build.Name;
                Status = $"Status: {d4BuildsStatusUpdateEventParams.Status}";
                Variants.AddRange(d4BuildsStatusUpdateEventParams.Build.Variants.Select(v => v.Name));
            });
        }

        private bool CanSetDoneExecute()
        {
            return _d4BuildsComplated;
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
