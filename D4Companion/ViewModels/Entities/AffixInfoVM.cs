using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Linq;

namespace D4Companion.ViewModels.Entities
{
    public class AffixInfoVM : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISystemPresetManager _systemPresetManager;

        private AffixInfo _affixInfo = new AffixInfo();

        // Start of Constructors region

        #region Constructors

        public AffixInfoVM(AffixInfo affixInfo)
        {
            _affixInfo = affixInfo;

            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
            _eventAggregator.GetEvent<SystemPresetMappingChangedEvent>().Subscribe(HandleSystemPresetMappingChangedEvent);

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

        public bool IsMappingReady
        {
            get
            {
                return _systemPresetManager.AffixMappings.Any(mapping => mapping.IdName.Equals(IdName));
            }
        }

        public AffixInfo Model
        {
            get => _affixInfo;
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleSystemPresetMappingChangedEvent()
        {
            RaisePropertyChanged(nameof(IsMappingReady));
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
