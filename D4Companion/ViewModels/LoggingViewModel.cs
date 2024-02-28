using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace D4Companion.ViewModels
{
    public class LoggingViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;

        private int? _badgeCount = null;

        // Start of Constructors region

        #region Constructors

        public LoggingViewModel(IEventAggregator eventAggregator, ILogger<LoggingViewModel> logger)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            eventAggregator.GetEvent<InfoOccurredEvent>().Subscribe(HandleInfoOccurredEvent);
            eventAggregator.GetEvent<WarningOccurredEvent>().Subscribe(HandleWarningOccurredEvent);
            eventAggregator.GetEvent<ErrorOccurredEvent>().Subscribe(HandleErrorOccurredEvent);
            eventAggregator.GetEvent<ExceptionOccurredEvent>().Subscribe(HandleExceptionOccurredEvent);

            // Init logger
            _logger = logger;

            // Init View commands
            ClearLogMessagesCommand = new DelegateCommand(ClearLogMessagesExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand ClearLogMessagesCommand { get; }

        public int? BadgeCount
        {
            get => _badgeCount;
            set
            {
                _badgeCount = value;
                RaisePropertyChanged(nameof(BadgeCount));
            }
        }

        public ObservableCollection<string> LogMessages { get; set; } = new ObservableCollection<string>();

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleInfoOccurredEvent(InfoOccurredEventParams infoOccurredEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                string previousMessage = LogMessages.Any() ? LogMessages.Last() : string.Empty;
                if (!previousMessage.Equals(infoOccurredEventParams.Message))
                {
                    LogMessages.Add(infoOccurredEventParams.Message);
                    BadgeCount = LogMessages.Count;
                }
            });
        }

        private void HandleWarningOccurredEvent(WarningOccurredEventParams warningOccurredEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                string previousMessage = LogMessages.Any() ? LogMessages.Last() : string.Empty;
                if (!previousMessage.Equals(warningOccurredEventParams.Message)) 
                {
                    LogMessages.Add(warningOccurredEventParams.Message);
                    BadgeCount = LogMessages.Count;
                }
            });
        }

        private void HandleErrorOccurredEvent(ErrorOccurredEventParams errorOccurredEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                string previousMessage = LogMessages.Any() ? LogMessages.Last() : string.Empty;
                if (!previousMessage.Equals(errorOccurredEventParams.Message))
                {
                    LogMessages.Add(errorOccurredEventParams.Message);
                    BadgeCount = LogMessages.Count;
                }
            });
        }

        private void HandleExceptionOccurredEvent(ExceptionOccurredEventParams exceptionOccurredEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                string previousMessage = LogMessages.Any() ? LogMessages.Last() : string.Empty;
                if (!previousMessage.Equals(exceptionOccurredEventParams.Message))
                {
                    LogMessages.Add(exceptionOccurredEventParams.Message);
                    BadgeCount = LogMessages.Count;
                }
            });
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void ClearLogMessagesExecute()
        {
            LogMessages.Clear();
            BadgeCount = LogMessages.Count;
        }

        #endregion
    }
}
