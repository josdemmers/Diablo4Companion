using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Extensions;
using D4Companion.Messages;
using D4Companion.ViewModels.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class D4BuildsDownloadViewModel : ObservableObject,
        IDisposable,
        IRecipient<D4BuildsCompletedMessage>,
        IRecipient<D4BuildsStatusUpdateMessage>
    {
        private ObservableCollection<string> _variants = new ObservableCollection<string>();

        private string _buildName = string.Empty;
        private bool _d4BuildsCompleted = false;
        private string _status = string.Empty;

        // Start of Constructors region

        #region Constructors

        public D4BuildsDownloadViewModel(Action<D4BuildsDownloadViewModel?> closeHandler)
        {
            // Init messages
            WeakReferenceMessenger.Default.RegisterAll(this);

            // Init view commands
            CloseCommand = new RelayCommand<D4BuildsDownloadViewModel>(closeHandler);
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

        public void Receive(D4BuildsCompletedMessage message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _d4BuildsCompleted = true;
                ((RelayCommand)SetDoneCommand).NotifyCanExecuteChanged();
                CloseCommand.Execute(this);
            });
        }

        public void Receive(D4BuildsStatusUpdateMessage message)
        {
            var d4BuildsStatusUpdateMessageParams = message.Value;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Variants.Clear();

                BuildName = d4BuildsStatusUpdateMessageParams.Build.Name;
                Status = $"Status: {d4BuildsStatusUpdateMessageParams.Status}";
                Variants.AddRange(d4BuildsStatusUpdateMessageParams.Build.Variants.Select(v => v.Name));
            });
        }

        private bool CanSetDoneExecute()
        {
            return _d4BuildsCompleted;
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