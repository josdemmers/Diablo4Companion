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
    public class D2CoreDownloadViewModel : ObservableObject,
        IDisposable,
        IRecipient<D2CoreCompletedMessage>,
        IRecipient<D2CoreStatusUpdateMessage>
    {
        private ObservableCollection<string> _variants = new ObservableCollection<string>();

        private string _buildName = string.Empty;
        private bool _d2CoreCompleted = false;
        private string _status = "Preparing browser instance.";

        // Start of Constructors region

        #region Constructors

        public D2CoreDownloadViewModel(Action<D2CoreDownloadViewModel?> closeHandler)
        {
            // Init messages
            WeakReferenceMessenger.Default.RegisterAll(this);

            // Init view commands
            CloseCommand = new RelayCommand<D2CoreDownloadViewModel>(closeHandler);
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

        public void Receive(D2CoreCompletedMessage message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _d2CoreCompleted = true;
                ((RelayCommand)SetDoneCommand).NotifyCanExecuteChanged();
                CloseCommand.Execute(this);
            });
        }

        public void Receive(D2CoreStatusUpdateMessage message)
        {
            var d2CoreStatusUpdateMessageParams = message.Value;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Variants.Clear();

                BuildName = d2CoreStatusUpdateMessageParams.Build.Name;
                Status = $"Status: {d2CoreStatusUpdateMessageParams.Status}";
                Variants.AddRange(d2CoreStatusUpdateMessageParams.Build.Data.Variants.Select(v => v.Name));
            });
        }

        private bool CanSetDoneExecute()
        {
            return _d2CoreCompleted;
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
