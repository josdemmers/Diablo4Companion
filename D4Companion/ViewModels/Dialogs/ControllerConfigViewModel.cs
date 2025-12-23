using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Extensions;
using D4Companion.Interfaces;
using D4Companion.Messages;
using D4Companion.ViewModels.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class ControllerConfigViewModel : ObservableObject
    {
        private readonly ISettingsManager _settingsManager;
        private readonly ISystemPresetManager _systemPresetManager;

        private ObservableCollection<ControllerImageVM> _availableImages = new ObservableCollection<ControllerImageVM>();
        private ObservableCollection<ControllerImageVM> _selectedImages = new ObservableCollection<ControllerImageVM>();

        // Start of Constructors region

        #region Constructors

        public ControllerConfigViewModel(Action<ControllerConfigViewModel?> closeHandler)
        {
            // Init services
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();
            _systemPresetManager = App.Current.Services.GetRequiredService<ISystemPresetManager>();

            // Init view commands
            AddControllerCommand = new RelayCommand<ControllerImageVM>(AddControllerExecute);
            CloseCommand = new RelayCommand<ControllerConfigViewModel>(closeHandler);
            RemoveControllerCommand = new RelayCommand<ControllerImageVM>(RemoveControllerExecute);
            ControllerConfigDoneCommand = new RelayCommand(ControllerConfigDoneExecute);

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

        public ICommand AddControllerCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand RemoveControllerCommand { get; }
        public ICommand ControllerConfigDoneCommand { get; }

        public ObservableCollection<ControllerImageVM> AvailableImages { get => _availableImages; set => _availableImages = value; }
        public ObservableCollection<ControllerImageVM> SelectedImages { get => _selectedImages; set => _selectedImages = value; }

        public string SelectedSystemPreset => _settingsManager.Settings.SelectedSystemPreset;

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void AddControllerExecute(ControllerImageVM? controllerImageVM)
        {
            if (!string.IsNullOrWhiteSpace(controllerImageVM?.FileName))
            {
                _systemPresetManager.AddController(controllerImageVM.FileName);
                LoadSelectedImages();
            }
        }

        private void RemoveControllerExecute(ControllerImageVM? controllerImageVM)
        {
            if (!string.IsNullOrWhiteSpace(controllerImageVM?.FileName))
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

            WeakReferenceMessenger.Default.Send(new AvailableImagesChangedMessage());
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
