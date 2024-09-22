using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Events;
using Prism.Mvvm;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace D4Companion.ViewModels.Entities
{
    public class AffixInfoBase : BindableBase
    {

    }

    public class AffixInfoConfig : AffixInfoBase
    {

    }

    public class AffixInfoWanted : AffixInfoBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISystemPresetManager _systemPresetManager;

        private AffixInfo _affixInfo = new AffixInfo();

        // Start of Constructors region

        #region Constructors

        public AffixInfoWanted(AffixInfo affixInfo)
        {
            _affixInfo = affixInfo;

            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _systemPresetManager = (ISystemPresetManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISystemPresetManager));
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<int> AllowedForPlayerClass 
        { 
            get => _affixInfo.AllowedForPlayerClass;
        }

        public string Description 
        {
            get => _affixInfo.Description;
        }

        public string IdName
        {
            get => _affixInfo.IdName;
        }

        public bool IsClassBarb
        {
            get => _affixInfo.AllowedForPlayerClass[2] == 1;
        }

        public bool IsClassDruid
        {
            get => _affixInfo.AllowedForPlayerClass[1] == 1;
        }

        public bool IsClassNecro
        {
            get => _affixInfo.AllowedForPlayerClass[4] == 1;
        }

        public bool IsClassRogue
        {
            get => _affixInfo.AllowedForPlayerClass[3] == 1;
        }

        public bool IsClassSorc
        {
            get => _affixInfo.AllowedForPlayerClass[0] == 1;
        }

        public bool IsClassSpiritborn
        {
            get => _affixInfo.AllowedForPlayerClass[5] == 1;
        }

        public bool IsTemperingAvailable
        {
            get => _affixInfo.IsTemperingAvailable;
        }

        public AffixInfo Model
        {
            get => _affixInfo;
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }

    public class AffixInfoCustomSort : IComparer
    {
        public int Compare(object? x, object? y)
        {
            int result = -1;

            if ((x.GetType() == typeof(AffixInfoConfig)) && !(y.GetType() == typeof(AffixInfoConfig))) return -1;
            if ((y.GetType() == typeof(AffixInfoConfig)) && !(x.GetType() == typeof(AffixInfoConfig))) return 1;

            if ((x.GetType() == typeof(AffixInfoWanted)) && (y.GetType() == typeof(AffixInfoWanted)))
            {
                var itemX = (AffixInfoWanted)x;
                var itemY = (AffixInfoWanted)y;

                result = itemX.Description.CompareTo(itemY.Description);
            }

            return result;
        }
    }
}
