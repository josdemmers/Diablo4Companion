using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.ViewModels.Dialogs;
using D4Companion.ViewModels.Entities;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace D4Companion.ViewModels
{
    public class TradeViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;
        private readonly ITradeItemManager _tradeItemManager;

        private ObservableCollection<TradeItemBase> _tradeItems = new ObservableCollection<TradeItemBase>();

        private int? _badgeCount = null;

        // Start of Constructors region

        #region Constructors

        public TradeViewModel(IEventAggregator eventAggregator, ILogger<TradeViewModel> logger, IDialogCoordinator dialogCoordinator,
            ISettingsManager settingsManager, ITradeItemManager tradeItemManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);
            _eventAggregator.GetEvent<ToggleCurrentItemEvent>().Subscribe(HandleToggleCurrentItemEvent);
            _eventAggregator.GetEvent<TooltipDataReadyEvent>().Subscribe(HandleTooltipDataReadyEvent);

            // Init logger
            _logger = logger;

            // Init services
            _dialogCoordinator = dialogCoordinator;
            _settingsManager = settingsManager;
            _tradeItemManager = tradeItemManager;

            // Init view commands
            AddTradeItemCommand = new DelegateCommand(AddTradeItemExecute);
            EditTradeItemCommand = new DelegateCommand<TradeItemWanted>(EditTradeItemExecute);
            RemoveTradeItemCommand = new DelegateCommand<TradeItemWanted>(RemoveTradeItemExecute);
            TradeConfigCommand = new DelegateCommand(TradeConfigExecute);

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

        public DelegateCommand AddTradeItemCommand { get; }
        public DelegateCommand<TradeItemWanted> EditTradeItemCommand { get; }
        public DelegateCommand<TradeItemWanted> RemoveTradeItemCommand { get; }
        public DelegateCommand TradeConfigCommand { get; }

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

        private async void EditTradeItemExecute(TradeItemWanted tradeItem)
        {
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

        private void HandleApplicationLoadedEvent()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TradeItems.AddRange(_tradeItemManager.TradeItems.Select(tradeItem => new TradeItemWanted(tradeItem)));
            });
        }


        private void HandleToggleCurrentItemEvent()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TradeItemsFiltered?.Refresh();
            });
        }

        private void HandleTooltipDataReadyEvent(TooltipDataReadyEventParams tooltipDataReadyEventParams)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var currentItem = TradeItems.FirstOrDefault(item => item.GetType() == typeof(TradeItemCurrent)) as TradeItemCurrent;
                if (currentItem != null && tooltipDataReadyEventParams.Tooltip.ItemAffixes.Count > 0) 
                {
                    currentItem.ItemType = tooltipDataReadyEventParams.Tooltip.OcrResultItemType.Type;
                    currentItem.ItemPower = tooltipDataReadyEventParams.Tooltip.ItemPower.ToString();
                    currentItem.Affixes = string.Join(System.Environment.NewLine, tooltipDataReadyEventParams.Tooltip.OcrResultAffixes
                        .Select(a => a.OcrResult.Text.ReplaceLineEndings().Replace(System.Environment.NewLine, string.Empty)));
                }
            });
        }

        private void RemoveTradeItemExecute(TradeItemWanted tradeItem)
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
