using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Events;
using Prism.Mvvm;

namespace D4Companion.ViewModels.Entities
{
    public class AffixImageVM : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISystemPresetManager _systemPresetManager;

        private string _fileName = string.Empty;
        private string _folder = string.Empty;
        
        // Start of Constructors region

        #region Constructors

        public AffixImageVM(string folder, string fileName)
        {
            _fileName = fileName;
            _folder = folder;

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

        public string FileName
        {
            get => _fileName;
        }

        public string Folder
        {
            get => _folder;
        }

        public int UsageCount
        {
            get => _systemPresetManager.GetImageUsageCount(Folder, FileName);
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleSystemPresetMappingChangedEvent()
        {
            RaisePropertyChanged(nameof(UsageCount));
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
