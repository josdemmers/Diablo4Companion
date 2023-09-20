using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.ViewModels.Entities
{
    public class SigilInfoVM : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISystemPresetManager _systemPresetManager;

        private SigilInfo _sigilInfo = new SigilInfo();

        // Start of Constructors region

        #region Constructors

        public SigilInfoVM(SigilInfo sigilInfo)
        {
            _sigilInfo = sigilInfo;

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

        public string Description
        {
            get => _sigilInfo.Description;
        }

        public string IdName
        {
            get => _sigilInfo.IdName;
        }

        public bool IsMappingReady
        {
            get
            {
                return _systemPresetManager.AffixMappings.Any(mapping => mapping.IdName.Equals(IdName));
            }
        }

        public SigilInfo Model
        {
            get => _sigilInfo;
        }

        public string Name
        {
            get => _sigilInfo.Name;
        }

        public string Type
        {
            get => _sigilInfo.Type;
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        private void HandleSystemPresetMappingChangedEvent()
        {
            RaisePropertyChanged(nameof(IsMappingReady));
        }

        #endregion
    }
}