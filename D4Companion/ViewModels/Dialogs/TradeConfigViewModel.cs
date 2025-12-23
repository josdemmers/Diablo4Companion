using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Interfaces;
using D4Companion.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class TradeConfigViewModel : ObservableObject
    {
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public TradeConfigViewModel(Action<TradeConfigViewModel?> closeHandler)
        {
            // Init services
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init view commands
            CloseCommand = new RelayCommand<TradeConfigViewModel>(closeHandler);
            TradeConfigDoneCommand = new RelayCommand(TradeConfigDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand TradeConfigDoneCommand { get; }

        public bool IsTradeOverlayEnabled
        {
            get => _settingsManager.Settings.IsTradeOverlayEnabled;
            set
            {
                _settingsManager.Settings.IsTradeOverlayEnabled = value;
                OnPropertyChanged(nameof(IsTradeOverlayEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public int OverlayFontSize
        {
            get => _settingsManager.Settings.OverlayFontSize;
            set
            {
                _settingsManager.Settings.OverlayFontSize = value;
                OnPropertyChanged(nameof(OverlayFontSize));

                _settingsManager.SaveSettings();
            }
        }

        public bool ShowCurrentItem
        {
            get => _settingsManager.Settings.ShowCurrentItem;
            set
            {
                _settingsManager.Settings.ShowCurrentItem = value;
                OnPropertyChanged(nameof(ShowCurrentItem));

                _settingsManager.SaveSettings();

                WeakReferenceMessenger.Default.Send(new ToggleCurrentItemMessage());
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void TradeConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
