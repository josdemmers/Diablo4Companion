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

namespace D4Companion.ViewModels
{
    public class AffixViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;

        private ObservableCollection<AffixInfo> _affixes = new ObservableCollection<AffixInfo>();

        private int? _badgeCount = null;

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
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixInfo> Affixes { get => _affixes; set => _affixes = value; }

        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }

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

        #endregion
    }
}
