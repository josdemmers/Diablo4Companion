using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Extensions;
using D4Companion.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class MobalyticsDownloadViewModel : ObservableObject,
        IDisposable,
        IRecipient<MobalyticsCompletedMessage>,
        IRecipient<MobalyticsStatusUpdateMessage>
    {
        private ObservableCollection<string> _variants = new ObservableCollection<string>();

        private string _buildName = string.Empty;
        private bool _mobalyticsCompleted = false;
        private string _status = "Preparing browser instance.";

        // Start of Constructors region

        #region Constructors

        public MobalyticsDownloadViewModel(Action<MobalyticsDownloadViewModel?> closeHandler)
        {
            // Init messages
            WeakReferenceMessenger.Default.RegisterAll(this);

            // Init view commands
            CloseCommand = new RelayCommand<MobalyticsDownloadViewModel>(closeHandler);
            SetDoneCommand = new RelayCommand(SetDoneExecute, CanSetDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand SetDoneCommand { get; }

        public ObservableCollection<string> Variants { get => _variants; set => _variants = value; }

        public string BuildName
        {
            get => _buildName;
            set
            {
                _buildName = value;
                OnPropertyChanged(nameof(BuildName));
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        public void Receive(MobalyticsCompletedMessage message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _mobalyticsCompleted = true;
                ((RelayCommand)SetDoneCommand).NotifyCanExecuteChanged();
                CloseCommand.Execute(this);
            });
        }

        public void Receive(MobalyticsStatusUpdateMessage message)
        {
            var mobalyticsStatusUpdateMessageParams = message.Value;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Variants.Clear();

                BuildName = mobalyticsStatusUpdateMessageParams.Build.Name;
                Status = $"Status: {mobalyticsStatusUpdateMessageParams.Status}";
                Variants.AddRange(mobalyticsStatusUpdateMessageParams.Build.Variants.Select(v => v.Name));
            });
        }

        private bool CanSetDoneExecute()
        {
            return _mobalyticsCompleted;
        }

        private void SetDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        #endregion
    }
}
