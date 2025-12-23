using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Interfaces;
using D4Companion.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;

namespace D4Companion.ViewModels.Entities
{
    public class SigilInfoBase : ObservableObject
    {

    }

    public class SigilInfoConfig : SigilInfoBase
    {

    }

    public class SigilInfoWanted : SigilInfoBase
    {
        private readonly IAffixManager _affixManager;
        private readonly ISettingsManager _settingsManager;

        private SigilInfo _sigilInfo = new SigilInfo();

        // Start of Constructors region

        #region Constructors

        public SigilInfoWanted(SigilInfo sigilInfo)
        {
            _sigilInfo = sigilInfo;

            // Init services
            _affixManager = App.Current.Services.GetRequiredService<IAffixManager>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init messages
            WeakReferenceMessenger.Default.Register<DungeonTiersEnabledChangedMessage>(this, HandleDungeonTiersEnabledChangedMessage);

            // Init view commands
            SetSigilDungeonTierCommand = new RelayCommand<string>(SetSigilDungeonTierExecute);
            SetSigilDungeonTierToNextCommand = new RelayCommand<SigilInfoWanted>(SetSigilDungeonTierToNextExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand SetSigilDungeonTierCommand { get; }
        public ICommand SetSigilDungeonTierToNextCommand { get; }

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

        private void HandleDungeonTiersEnabledChangedMessage(object recipient, DungeonTiersEnabledChangedMessage message)
        {
            OnPropertyChanged(nameof(IsTierInfoEnabled));
        }

        private void SetSigilDungeonTierExecute(string? tier)
        {
            if (!string.IsNullOrWhiteSpace(tier))
            {
                _affixManager.SetSigilDungeonTier(Model, tier);
                OnPropertyChanged(nameof(Tier));
            }
        }

        private void SetSigilDungeonTierToNextExecute(SigilInfoWanted? sigilInfo)
        {
            if (sigilInfo != null)
            {
                int tierIndex = sigilInfo.Tiers.IndexOf(sigilInfo.Tier);
                int nextTierIndex = (tierIndex + 1) % sigilInfo.Tiers.Count;
                string nextTier = sigilInfo.Tiers[nextTierIndex];
                _affixManager.SetSigilDungeonTier(sigilInfo.Model, nextTier);
                OnPropertyChanged(nameof(Tier));
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }

    public class SigilInfoCustomSort : IComparer
    {
        public int Compare(object? x, object? y)
        {
            int result = -1;

            if (x == null) return -1;
            if (y == null) return 1;
            if ((x.GetType() == typeof(SigilInfoConfig)) && !(y.GetType() == typeof(SigilInfoConfig))) return -1;
            if ((y.GetType() == typeof(SigilInfoConfig)) && !(x.GetType() == typeof(SigilInfoConfig))) return 1;

            if ((x.GetType() == typeof(SigilInfoWanted)) && (y.GetType() == typeof(SigilInfoWanted)))
            {
                var itemX = (SigilInfoWanted)x;
                var itemY = (SigilInfoWanted)y;

                result = itemX.Name.CompareTo(itemY.Name);
            }

            return result;
        }
    }
}