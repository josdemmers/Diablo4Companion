using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;

namespace D4Companion.ViewModels.Dialogs
{
    public class TradeConfigViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public TradeConfigViewModel(Action<TradeConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            CloseCommand = new DelegateCommand<TradeConfigViewModel>(closeHandler);
            TradeConfigDoneCommand = new DelegateCommand(TradeConfigDoneExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<TradeConfigViewModel> CloseCommand { get; }
        public DelegateCommand TradeConfigDoneCommand { get; }

        public bool IsTradeOverlayEnabled
        {
            get => _settingsManager.Settings.IsTradeOverlayEnabled;
            set
            {
                _settingsManager.Settings.IsTradeOverlayEnabled = value;
                RaisePropertyChanged(nameof(IsTradeOverlayEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool ShowCurrentItem
        {
            get => _settingsManager.Settings.ShowCurrentItem;
            set
            {
                _settingsManager.Settings.ShowCurrentItem = value;
                RaisePropertyChanged(nameof(ShowCurrentItem));

                _settingsManager.SaveSettings();

                _eventAggregator.GetEvent<ToggleCurrentItemEvent>().Publish();
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
