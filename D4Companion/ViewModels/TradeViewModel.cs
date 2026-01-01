using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Extensions;
using D4Companion.Interfaces;
using D4Companion.Messages;
using D4Companion.ViewModels.Dialogs;
using D4Companion.ViewModels.Entities;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace D4Companion.ViewModels
{
    public class TradeViewModel : ObservableObject
    {
        private readonly ILogger _logger;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;
        private readonly ITradeItemManager _tradeItemManager;

        private ObservableCollection<TradeItemBase> _tradeItems = new ObservableCollection<TradeItemBase>();

        private int? _badgeCount = null;

        // Start of Constructors region

        #region Constructors

        public TradeViewModel(ILogger<TradeViewModel> logger, IDialogCoordinator dialogCoordinator,
            ISettingsManager settingsManager, ITradeItemManager tradeItemManager)
        {
            // Init services
            _dialogCoordinator = dialogCoordinator;
            _logger = logger;
            _settingsManager = settingsManager;
            _tradeItemManager = tradeItemManager;

            // Init messages
            WeakReferenceMessenger.Default.Register<ApplicationLoadedMessage>(this, HandleApplicationLoadedMessage);
            WeakReferenceMessenger.Default.Register<ToggleCurrentItemMessage>(this, HandleToggleCurrentItemMessage);
            WeakReferenceMessenger.Default.Register<TooltipDataReadyMessage>(this, HandleTooltipDataReadyMessage);

            // Init view commands
            AddTradeItemCommand = new RelayCommand(AddTradeItemExecute);
            EditTradeItemCommand = new RelayCommand<TradeItemWanted>(EditTradeItemExecute);
            RemoveTradeItemCommand = new RelayCommand<TradeItemWanted>(RemoveTradeItemExecute);
            TradeConfigCommand = new RelayCommand(TradeConfigExecute);

            // Init trading
            InitTradeItems();

            // Init filter views
            CreateTradeItemsFilteredView();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<TradeItemBase> TradeItems { get => _tradeItems; set => _tradeItems = value; }
        public ListCollectionView? TradeItemsFiltered { get; private set; }

        public ICommand AddTradeItemCommand { get; }
        public ICommand EditTradeItemCommand { get; }
        public ICommand RemoveTradeItemCommand { get; }
        public ICommand TradeConfigCommand { get; }

        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private async void AddTradeItemExecute()
        {
            bool editMode = false;
            TradeItemWanted tradeItem = new TradeItemWanted();

            var addTradeItemDialog = new CustomDialog() { Title = "Trade item" };
            var dataContext = new AddTradeItemViewModel(async instance =>
            {
                await addTradeItemDialog.WaitUntilUnloadedAsync();
            }, tradeItem, editMode);
            addTradeItemDialog.Content = new AddTradeItemView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, addTradeItemDialog);
            await addTradeItemDialog.WaitUntilUnloadedAsync();

            // Add confirmed trade item.
            if (!dataContext.IsCanceled)
            {
                TradeItems.Add(tradeItem);
                _tradeItemManager.SaveTradeItems(TradeItems.OfType<TradeItemWanted>().Select(tradeItem => tradeItem.AsTradeItem()).ToList());
            }
        }

        private async void EditTradeItemExecute(TradeItemWanted? tradeItem)
        {
            if (tradeItem == null) return;

            bool editMode = true;
            var addTradeItemDialog = new CustomDialog() { Title = "Trade item" };
            var dataContext = new AddTradeItemViewModel(async instance =>
            {
                await addTradeItemDialog.WaitUntilUnloadedAsync();
            }, tradeItem, editMode);
            addTradeItemDialog.Content = new AddTradeItemView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, addTradeItemDialog);
            await addTradeItemDialog.WaitUntilUnloadedAsync();

            // Type info is not updated because of Type.Image and Type.Name bindings. Refresh TradeItems list.
            TradeItemsFiltered?.Refresh();
            _tradeItemManager.SaveTradeItems(TradeItems.OfType<TradeItemWanted>().Select(tradeItem => tradeItem.AsTradeItem()).ToList());
        }

        private void HandleApplicationLoadedMessage(object recipient, ApplicationLoadedMessage message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TradeItems.AddRange(_tradeItemManager.TradeItems.Select(tradeItem => new TradeItemWanted(tradeItem)));
            });
        }

        private void HandleToggleCurrentItemMessage(object recipient, ToggleCurrentItemMessage message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TradeItemsFiltered?.Refresh();
            });
        }

        private void HandleTooltipDataReadyMessage(object recipient, TooltipDataReadyMessage message)
        {
            var tooltipDataReadyMessageParams = message.Value;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var currentItem = TradeItems.FirstOrDefault(item => item.GetType() == typeof(TradeItemCurrent)) as TradeItemCurrent;
                if (currentItem != null && tooltipDataReadyMessageParams.Tooltip.ItemAffixes.Count > 0)
                {
                    currentItem.ItemType = tooltipDataReadyMessageParams.Tooltip.OcrResultItemType.Type;
                    currentItem.ItemPower = tooltipDataReadyMessageParams.Tooltip.ItemPower.ToString();
                    currentItem.Affixes = string.Join(System.Environment.NewLine, tooltipDataReadyMessageParams.Tooltip.OcrResultAffixes
                        .Select(a => a.OcrResult.Text.ReplaceLineEndings().Replace(System.Environment.NewLine, string.Empty)));
                }
            });
        }

        private void RemoveTradeItemExecute(TradeItemWanted? tradeItem)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (tradeItem != null)
                {
                    TradeItems.Remove(tradeItem);
                    _tradeItemManager.SaveTradeItems(TradeItems.OfType<TradeItemWanted>().Select(tradeItem => tradeItem.AsTradeItem()).ToList());
                }
            });
        }

        private async void TradeConfigExecute()
        {
            var tradeConfigDialog = new CustomDialog() { Title = "Trade config" };
            var dataContext = new TradeConfigViewModel(async instance =>
            {
                await tradeConfigDialog.WaitUntilUnloadedAsync();
            });
            tradeConfigDialog.Content = new TradeConfigView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, tradeConfigDialog);
            await tradeConfigDialog.WaitUntilUnloadedAsync();
        }


        #endregion

        // Start of Methods region

        #region Methods

        private void CreateTradeItemsFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                TradeItemsFiltered = new ListCollectionView(TradeItems)
                {
                    Filter = FilterTradeItems
                };

                TradeItemsFiltered.CustomSort = new TradeItemCustomSort();
            });
        }

        private bool FilterTradeItems(object tradeItemObj)
        {
            var allowed = true;
            if (tradeItemObj == null) return false;
            if (tradeItemObj.GetType() == typeof(TradeItemCurrent) && !_settingsManager.Settings.ShowCurrentItem) return false;

            return allowed;
        }

        private void InitTradeItems()
        {
            TradeItems.Clear();
            TradeItems.Add(new TradeItemAdd());
            TradeItems.Add(new TradeItemCurrent());
        }

        #endregion
    }
}
