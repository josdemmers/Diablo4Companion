using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            eventAggregator.GetEvent<ErrorOccurredEvent>().Subscribe(HandleErrorOccurredEvent);
            eventAggregator.GetEvent<ExceptionOccurredEvent>().Subscribe(HandleExceptionOccurredEvent);

            // Init logger
            _logger = logger;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

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


        private void HandleErrorOccurredEvent(ErrorOccurredEventParams errorOccurredEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                LogMessages.Add(errorOccurredEventParams.Message);
                BadgeCount = LogMessages.Count;
            });
        }

        private void HandleExceptionOccurredEvent(ExceptionOccurredEventParams exceptionOccurredEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                LogMessages.Add(exceptionOccurredEventParams.Message);
                BadgeCount = LogMessages.Count;
            });
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
