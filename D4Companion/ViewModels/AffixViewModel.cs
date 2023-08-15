using D4Companion.Entities;
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
using System.Windows.Data;

namespace D4Companion.ViewModels
{
    public class AffixViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;

        private ObservableCollection<AffixInfo> _affixes = new ObservableCollection<AffixInfo>();

        private string _affixTextFilter = string.Empty;
        private int? _badgeCount = null;
        private bool _toggleCore = false;
        private bool _toggleBarbarian = false;
        private bool _toggleDruid = false;
        private bool _toggleNecromancer = false;
        private bool _toggleRogue = false;
        private bool _toggleSorcerer = false;

        // Start of Constructors region

        #region Constructors

        public AffixViewModel(IEventAggregator eventAggregator, ILogger<AffixViewModel> logger, IAffixManager affixManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);

            // Init logger
            _logger = logger;

            // Init services
            _affixManager = affixManager;

            // Init filter views
            CreateItemAffixesFilteredView();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixInfo> Affixes { get => _affixes; set => _affixes = value; }
        public ListCollectionView? AffixesFiltered { get; private set; }

        public string AffixTextFilter
        {
            get => _affixTextFilter;
            set
            {
                SetProperty(ref _affixTextFilter, value, () => { RaisePropertyChanged(nameof(AffixTextFilter)); });
                AffixesFiltered?.Refresh();
            }
        }
        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }
        
        public bool ToggleCore
        {
            get => _toggleCore; set
            {
                _toggleCore = value;

                if (value) 
                {
                    ToggleBarbarian = false;
                    ToggleDruid = false;
                    ToggleNecromancer = false;
                    ToggleRogue = false;
                    ToggleSorcerer = false;

                    AffixesFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleCore));
            }
        }

        /// <summary>
        /// Reset filter when all category toggles are false.
        /// </summary>
        private void CheckResetAffixFilter()
        {
            if (!ToggleCore && !ToggleBarbarian && !ToggleDruid && !ToggleNecromancer && !ToggleRogue && !ToggleSorcerer) 
            {
                AffixesFiltered?.Refresh();
            }
        }

        public bool ToggleBarbarian
        {
            get => _toggleBarbarian;
            set
            {
                _toggleBarbarian = value;

                if (value)
                {
                    ToggleCore = false;
                    ToggleDruid = false;
                    ToggleNecromancer = false;
                    ToggleRogue = false;
                    ToggleSorcerer = false;

                    AffixesFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleBarbarian));
            }
        }

        public bool ToggleDruid
        {
            get => _toggleDruid; set
            {
                _toggleDruid = value;

                if (value)
                {
                    ToggleCore = false;
                    ToggleBarbarian = false;
                    ToggleNecromancer = false;
                    ToggleRogue = false;
                    ToggleSorcerer = false;

                    AffixesFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleDruid));
            }
        }

        public bool ToggleNecromancer
        {
            get => _toggleNecromancer;
            set
            {
                _toggleNecromancer = value;

                if (value)
                {
                    ToggleCore = false;
                    ToggleBarbarian = false;
                    ToggleDruid = false;
                    ToggleRogue = false;
                    ToggleSorcerer = false;

                    AffixesFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleNecromancer));
            }
        }

        public bool ToggleRogue
        {
            get => _toggleRogue;
            set
            {
                _toggleRogue = value;

                if (value)
                {
                    ToggleCore = false;
                    ToggleBarbarian = false;
                    ToggleDruid = false;
                    ToggleNecromancer = false;
                    ToggleSorcerer = false;

                    AffixesFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleRogue));
            }
        }

        public bool ToggleSorcerer
        {
            get => _toggleSorcerer;
            set
            {
                _toggleSorcerer = value;

                if (value)
                {
                    ToggleCore = false;
                    ToggleBarbarian = false;
                    ToggleDruid = false;
                    ToggleNecromancer = false;
                    ToggleRogue = false;

                    AffixesFiltered?.Refresh();
                }

                CheckResetAffixFilter();
                RaisePropertyChanged(nameof(ToggleSorcerer));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleApplicationLoadedEvent()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Affixes.Clear();
                Affixes.AddRange(_affixManager.Affixes);
            });
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void CreateItemAffixesFilteredView()
        {
            // As the view is accessed by the UI it will need to be created on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                AffixesFiltered = new ListCollectionView(Affixes)
                {
                    Filter = FilterAffixes
                };
            });
        }

        private bool FilterAffixes(object affixObj)
        {
            var allowed = true;
            if (affixObj == null) return false;

            AffixInfo affixInfo = (AffixInfo)affixObj;

            if (!affixInfo.Description.ToLower().Contains(AffixTextFilter.ToLower()) && !string.IsNullOrWhiteSpace(AffixTextFilter))
            {
                return false;
            }

            if (ToggleCore)
            {
                allowed = affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleBarbarian)
            {
                allowed = affixInfo.AllowedForPlayerClass[2] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleDruid)
            {
                allowed = affixInfo.AllowedForPlayerClass[1] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleNecromancer)
            {
                allowed = affixInfo.AllowedForPlayerClass[4] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleRogue)
            {
                allowed = affixInfo.AllowedForPlayerClass[3] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }
            else if (ToggleSorcerer)
            {
                allowed = affixInfo.AllowedForPlayerClass[0] == 1 && !affixInfo.AllowedForPlayerClass.All(c => c == 1);
            }

            return allowed;
        }

        #endregion
    }
}
