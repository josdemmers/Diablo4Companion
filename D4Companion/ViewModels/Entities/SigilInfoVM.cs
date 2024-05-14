using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;

namespace D4Companion.ViewModels.Entities
{
    public class SigilInfoVM : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IAffixManager _affixManager;
        private readonly ISettingsManager _settingsManager;

        private SigilInfo _sigilInfo = new SigilInfo();

        // Start of Constructors region

        #region Constructors

        public SigilInfoVM(SigilInfo sigilInfo)
        {
            _sigilInfo = sigilInfo;

            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
            _eventAggregator.GetEvent<SelectedSigilDungeonTierChangedEvent>().Subscribe(HandleSelectedSigilDungeonTierChangedEvent);

            // Init services
            _affixManager = (IAffixManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAffixManager));
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));

            // Init View commands
            SetSigilDungeonTierCommand = new DelegateCommand<string>(SetSigilDungeonTierExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<string> SetSigilDungeonTierCommand { get; }

        public List<string> Tiers { get; set; } = new List<string>()
        {
            "S",
            "A",
            "B",
            "C",
            "D",
            "E",
            "F"
        };

        public string Description
        {
            get => _sigilInfo.Description;
        }

        public string IdName
        {
            get => _sigilInfo.IdName;
        }

        public SigilInfo Model
        {
            get => _sigilInfo;
        }

        public string Name
        {
            get => _sigilInfo.Name;
        }

        public string Tier
        {
            get => _affixManager.GetSigilDungeonTier(IdName);
        }

        public bool IsSeasonal 
        {
            get => _sigilInfo.IsSeasonal;
        }

        public bool IsTierInfoEnabled
        {
            get => Type.Equals(Constants.SigilTypeConstants.Dungeon) &&
                _settingsManager.Settings.DungeonTiers;
        }

        public string Type
        {
            get => _sigilInfo.Type;
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleSelectedSigilDungeonTierChangedEvent()
        {
            RaisePropertyChanged(nameof(IsTierInfoEnabled));
            RaisePropertyChanged(nameof(Tier));
        }

        private void SetSigilDungeonTierExecute(string tier)
        {
            if (!string.IsNullOrWhiteSpace(tier))
            {
                _affixManager.SetSigilDungeonTier(Model, tier);
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}