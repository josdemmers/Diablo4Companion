using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.ViewModels.Entities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace D4Companion.ViewModels.Dialogs
{
    public class SetAffixMappingViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettingsManager _settingsManager;
        private readonly ISystemPresetManager _systemPresetManager;

        private ObservableCollection<AffixImageVM> _availableImages = new ObservableCollection<AffixImageVM>();
        private ObservableCollection<AffixImageVM> _selectedImages = new ObservableCollection<AffixImageVM>();

        private AffixInfo _affixInfo = new AffixInfo();
        private string _affixTextFilter = string.Empty;

        // Start of Constructors region

        #region Constructors

        public SetAffixMappingViewModel(Action<SetAffixMappingViewModel> closeHandler, AffixInfo affixInfo)
        {
            _affixInfo = affixInfo;

            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));
            _systemPresetManager = (ISystemPresetManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISystemPresetManager));

            // Init View commands
            AddMappingCommand = new DelegateCommand<AffixImageVM>(AddMappingExecute);
            CloseCommand = new DelegateCommand<SetAffixMappingViewModel>(closeHandler);
            RemoveMappingCommand = new DelegateCommand<AffixImageVM>(RemoveMappingExecute);
            SetDoneCommand = new DelegateCommand(SetDoneExecute);

            // Init filter views
            CreateAvailableImagesFilteredView();

            // Load data
            LoadAvailableImages();
            LoadSelectedImages();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<AffixImageVM> AddMappingCommand { get; }
        public DelegateCommand<SetAffixMappingViewModel> CloseCommand { get; }
        public DelegateCommand<AffixImageVM> RemoveMappingCommand { get; }
        public DelegateCommand SetDoneCommand { get; }

        public ObservableCollection<AffixImageVM> AvailableImages { get => _availableImages; set => _availableImages = value; }
        public ObservableCollection<AffixImageVM> SelectedImages { get => _selectedImages; set => _selectedImages = value; }
        public ListCollectionView? AvailableImagesFiltered { get; private set; }
        public ListCollectionView? SelectedImagesFiltered { get; private set; }


        public AffixInfo AffixInfo
        {
            get => _affixInfo;
            set
            {
                _affixInfo = value;
                RaisePropertyChanged();
            }
        }

        public string AffixTextFilter
        {
            get => _affixTextFilter;
            set
            {
                SetProperty(ref _affixTextFilter, value, () => { RaisePropertyChanged(nameof(AffixTextFilter)); });
                AvailableImagesFiltered?.Refresh();
            }
        }

        public string SelectedSystemPreset => _settingsManager.Settings.SelectedSystemPreset;

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void AddMappingExecute(AffixImageVM affixImageVM)
        {
            if (!string.IsNullOrWhiteSpace(affixImageVM.FileName))
            {
                _systemPresetManager.AddMapping(_affixInfo.IdName, affixImageVM.Folder, affixImageVM.FileName);
                LoadSelectedImages();

                _eventAggregator.GetEvent<SystemPresetMappingChangedEvent>().Publish();
            }
        }

        private void RemoveMappingExecute(AffixImageVM affixImageVM)
        {
            if (!string.IsNullOrWhiteSpace(affixImageVM.FileName))
            {
                _systemPresetManager.RemoveMapping(_affixInfo.IdName, affixImageVM.Folder, affixImageVM.FileName);
                LoadSelectedImages();

                _eventAggregator.GetEvent<SystemPresetMappingChangedEvent>().Publish();
            }
        }

        private void SetDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void CreateAvailableImagesFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                AvailableImagesFiltered = new ListCollectionView(AvailableImages)
                {
                    Filter = FilterAvailableImages
                };
            });
        }

        private bool FilterAvailableImages(object availableImageObj)
        {
            bool allowed = true;
            if (availableImageObj == null) return false;

            AffixImageVM availableImage = (AffixImageVM)availableImageObj;

            if (!availableImage.FileName.Contains(AffixTextFilter, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(AffixTextFilter))
            {
                return false;
            }

            return allowed;
        }

        private void LoadAvailableImages()
        {
            AvailableImages.Clear();
            AvailableImages.AddRange(_systemPresetManager.AffixEquipmentImages.Select(availableImage => new AffixImageVM("Affixes\\Equipment", availableImage)));
        }

        private void LoadSelectedImages()
        {
            SelectedImages.Clear();

            var mappings = _systemPresetManager.AffixMappings;
            var mapping = mappings.FirstOrDefault(mapping => mapping.IdName.Equals(AffixInfo.IdName));

            if (mapping != null)
            {
                SelectedImages.AddRange(mapping.Images.Select(availableImage => new AffixImageVM("Affixes\\Equipment", availableImage)));
            }
        }

        #endregion
    }
}
