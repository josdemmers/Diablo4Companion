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

namespace D4Companion.ViewModels.Dialogs
{
    public class ControllerConfigViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettingsManager _settingsManager;
        private readonly ISystemPresetManager _systemPresetManager;

        private ObservableCollection<ControllerImageVM> _availableImages = new ObservableCollection<ControllerImageVM>();
        private ObservableCollection<ControllerImageVM> _selectedImages = new ObservableCollection<ControllerImageVM>();

        // Start of Constructors region

        #region Constructors

        public ControllerConfigViewModel(Action<ControllerConfigViewModel> closeHandler)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));
            _systemPresetManager = (ISystemPresetManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISystemPresetManager));

            // Init View commands
            AddControllerCommand = new DelegateCommand<ControllerImageVM>(AddControllerExecute);
            CloseCommand = new DelegateCommand<ControllerConfigViewModel>(closeHandler);
            RemoveControllerCommand = new DelegateCommand<ControllerImageVM>(RemoveControllerExecute);
            ControllerConfigDoneCommand = new DelegateCommand(ControllerConfigDoneExecute);

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

        public DelegateCommand<ControllerImageVM> AddControllerCommand { get; }
        public DelegateCommand<ControllerConfigViewModel> CloseCommand { get; }
        public DelegateCommand<ControllerImageVM> RemoveControllerCommand { get; }
        public DelegateCommand ControllerConfigDoneCommand { get; }

        public ObservableCollection<ControllerImageVM> AvailableImages { get => _availableImages; set => _availableImages = value; }
        public ObservableCollection<ControllerImageVM> SelectedImages { get => _selectedImages; set => _selectedImages = value; }

        public string SelectedSystemPreset => _settingsManager.Settings.SelectedSystemPreset;

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void AddControllerExecute(ControllerImageVM controllerImageVM)
        {
            if (!string.IsNullOrWhiteSpace(controllerImageVM.FileName))
            {
                _systemPresetManager.AddController(controllerImageVM.FileName);
                LoadSelectedImages();
            }
        }

        private void RemoveControllerExecute(ControllerImageVM controllerImageVM)
        {
            if (!string.IsNullOrWhiteSpace(controllerImageVM.FileName))
            {
                _systemPresetManager.RemoveController(controllerImageVM.FileName);
                LoadSelectedImages();
            }
        }

        private void ControllerConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void LoadAvailableImages()
        {
            // Reload available images
            _systemPresetManager.LoadControllerImages();

            // Add available images
            AvailableImages.Clear();
            AvailableImages.AddRange(_systemPresetManager.ControllerImages.Select(availableImage => new ControllerImageVM("Tooltips", availableImage)));

            // Notify subscribers available images have changed
            _eventAggregator.GetEvent<AvailableImagesChangedEvent>().Publish();
        }

        private void LoadSelectedImages()
        {
            SelectedImages.Clear();

            var controllerConfig = _systemPresetManager.ControllerConfig;

            if (controllerConfig != null)
            {
                SelectedImages.AddRange(controllerConfig.Select(c => new ControllerImageVM("Tooltips", c)));
            }
        }

        #endregion
    }
}
