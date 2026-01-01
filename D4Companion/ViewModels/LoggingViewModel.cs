using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace D4Companion.ViewModels
{
    public class LoggingViewModel : ObservableObject
    {
        private readonly ILogger _logger;

        private int? _badgeCount = null;

        // Start of Constructors region

        #region Constructors

        public LoggingViewModel(ILogger<LoggingViewModel> logger)
        {
            // Init services
            _logger = logger;

            // Init messages
            WeakReferenceMessenger.Default.Register<InfoOccurredMessage>(this, HandleInfoOccurredMessage);
            WeakReferenceMessenger.Default.Register<WarningOccurredMessage>(this, HandleWarningOccurredMessage);
            WeakReferenceMessenger.Default.Register<ErrorOccurredMessage>(this, HandleErrorOccurredMessage);
            WeakReferenceMessenger.Default.Register<ExceptionOccurredMessage>(this, HandleExceptionOccurredMessage);

            // Init view commands
            ClearLogMessagesCommand = new RelayCommand(ClearLogMessagesExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand ClearLogMessagesCommand { get; }

        public int? BadgeCount
        {
            get => _badgeCount;
            set
            {
                _badgeCount = value;
                OnPropertyChanged(nameof(BadgeCount));
            }
        }

        public ObservableCollection<string> LogMessages { get; set; } = new ObservableCollection<string>();

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleInfoOccurredMessage(object recipient, InfoOccurredMessage message)
        {
            var infoOccurredMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                string previousMessage = LogMessages.Any() ? LogMessages.Last() : string.Empty;
                if (!previousMessage.Equals(infoOccurredMessageParams.Message))
                {
                    LogMessages.Add(infoOccurredMessageParams.Message);
                    BadgeCount = LogMessages.Count;
                }
            });
        }

        private void HandleWarningOccurredMessage(object recipient, WarningOccurredMessage message)
        {
            var warningOccurredMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                string previousMessage = LogMessages.Any() ? LogMessages.Last() : string.Empty;
                if (!previousMessage.Equals(warningOccurredMessageParams.Message))
                {
                    LogMessages.Add(warningOccurredMessageParams.Message);
                    BadgeCount = LogMessages.Count;
                }
            });
        }

        private void HandleErrorOccurredMessage(object recipient, ErrorOccurredMessage message)
        {
            var errorOccurredMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                string previousMessage = LogMessages.Any() ? LogMessages.Last() : string.Empty;
                if (!previousMessage.Equals(errorOccurredMessageParams.Message))
                {
                    LogMessages.Add(errorOccurredMessageParams.Message);
                    BadgeCount = LogMessages.Count;
                }
            });
        }

        private void HandleExceptionOccurredMessage(object recipient, ExceptionOccurredMessage message)
        {
            var exceptionOccurredMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                string previousMessage = LogMessages.Any() ? LogMessages.Last() : string.Empty;
                if (!previousMessage.Equals(exceptionOccurredMessageParams.Message))
                {
                    LogMessages.Add(exceptionOccurredMessageParams.Message);
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
