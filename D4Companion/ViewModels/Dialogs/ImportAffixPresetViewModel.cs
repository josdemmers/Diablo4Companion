using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace D4Companion.ViewModels.Dialogs
{
    public class ImportAffixPresetViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly IBuildsManager _buildsManager;
        private readonly IDialogCoordinator _dialogCoordinator;

        private ObservableCollection<AffixPreset> _affixPresets = new();
        private ObservableCollection<MaxrollBuild> _maxrollBuilds = new();

        private string _buildId = string.Empty;
        private AffixPreset _selectedAffixPreset = new AffixPreset();
        private MaxrollBuild _selectedMaxrollBuild = new();

        // Start of Constructors region

        #region Constructors

        public ImportAffixPresetViewModel(Action<ImportAffixPresetViewModel> closeHandler, IAffixManager affixManager, IBuildsManager buildsManager)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
            _eventAggregator.GetEvent<AffixPresetAddedEvent>().Subscribe(HandleAffixPresetAddedEvent);
            _eventAggregator.GetEvent<AffixPresetRemovedEvent>().Subscribe(HandleAffixPresetRemovedEvent);
            _eventAggregator.GetEvent<MaxrollBuildsLoadedEvent>().Subscribe(HandleMaxrollBuildsLoadedEvent);

            // Init services
            _logger = (ILogger<ImportAffixPresetViewModel>)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ILogger<ImportAffixPresetViewModel>));
            _affixManager = affixManager;
            _buildsManager = buildsManager;
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));

            // Init View commands
            AddMaxrollBuildCommand = new DelegateCommand(AddMaxrollBuildExecute, CanAddMaxrollBuildExecute);
            AddMaxrollBuildAsPresetCommand = new DelegateCommand<MaxrollBuildDataProfileJson>(AddMaxrollBuildAsPresetExecute);
            CloseCommand = new DelegateCommand<ImportAffixPresetViewModel>(closeHandler);
            ImportAffixPresetDoneCommand = new DelegateCommand(ImportAffixPresetDoneExecute);
            RemoveAffixPresetNameCommand = new DelegateCommand(RemoveAffixPresetNameExecute, CanRemoveAffixPresetNameExecute);
            RemoveMaxrollBuildCommand = new DelegateCommand<MaxrollBuild>(RemoveMaxrollBuildExecute);
            SelectMaxrollBuildCommand = new DelegateCommand<MaxrollBuild>(SelectMaxrollBuildExecute);
            UpdateMaxrollBuildCommand = new DelegateCommand<MaxrollBuild>(UpdateMaxrollBuildExecute);
            WebMaxrollBuildCommand = new DelegateCommand<MaxrollBuild>(WebMaxrollBuildExecute);

            // Load affix presets
            UpdateAffixPresets();

            // Load Maxroll builds
            UpdateMaxrollBuilds();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixPreset> AffixPresets { get => _affixPresets; set => _affixPresets = value; }
        public ObservableCollection<MaxrollBuild> MaxrollBuilds { get => _maxrollBuilds; set => _maxrollBuilds = value; }

        public DelegateCommand AddMaxrollBuildCommand { get; }
        public DelegateCommand<MaxrollBuildDataProfileJson> AddMaxrollBuildAsPresetCommand { get; }
        public DelegateCommand<ImportAffixPresetViewModel> CloseCommand { get; }
        public DelegateCommand ImportAffixPresetDoneCommand { get; }
        public DelegateCommand RemoveAffixPresetNameCommand { get; }
        public DelegateCommand<MaxrollBuild> RemoveMaxrollBuildCommand { get; }
        public DelegateCommand<MaxrollBuild> SelectMaxrollBuildCommand { get; }
        public DelegateCommand<MaxrollBuild> UpdateMaxrollBuildCommand { get; }
        public DelegateCommand<MaxrollBuild> WebMaxrollBuildCommand { get; }

        public string BuildId
        {
            get => _buildId;
            set
            {
                _buildId = value;
                RaisePropertyChanged(nameof(BuildId));
                AddMaxrollBuildCommand?.RaiseCanExecuteChanged();
            }
        }

        public AffixPreset SelectedAffixPreset
        {
            get => _selectedAffixPreset;
            set
            {
                _selectedAffixPreset = value;
                if (value == null)
                {
                    _selectedAffixPreset = new AffixPreset();
                }
                RaisePropertyChanged(nameof(SelectedAffixPreset));
                RemoveAffixPresetNameCommand?.RaiseCanExecuteChanged();
            }
        }

        public MaxrollBuild SelectedMaxrollBuild
        {
            get => _selectedMaxrollBuild;
            set
            {
                _selectedMaxrollBuild = value;
                if (value == null)
                {
                    _selectedMaxrollBuild = new();
                }
                RaisePropertyChanged(nameof(SelectedMaxrollBuild));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private bool CanAddMaxrollBuildExecute()
        {
            return !string.IsNullOrWhiteSpace(BuildId) && BuildId.Length == 8 && !BuildId.Contains("#");
        }

        private void AddMaxrollBuildExecute()
        {
            _buildsManager.DownloadMaxrollBuild(BuildId);
        }

        private void AddMaxrollBuildAsPresetExecute(MaxrollBuildDataProfileJson maxrollBuildDataProfileJson)
        {
            _buildsManager.CreatePresetFromMaxrollBuild(SelectedMaxrollBuild, maxrollBuildDataProfileJson.Name);
        }

        private void HandleAffixPresetAddedEvent()
        {
            UpdateAffixPresets();

            // Select added preset
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(SelectedMaxrollBuild.Name));
            if (preset != null)
            {
                SelectedAffixPreset = preset;
            }
        }

        private void HandleAffixPresetRemovedEvent()
        {
            UpdateAffixPresets();

            // Select first preset
            if (AffixPresets.Count > 0)
            {
                SelectedAffixPreset = AffixPresets[0];
            }
        }

        private void HandleMaxrollBuildsLoadedEvent()
        {
            UpdateMaxrollBuilds();
        }

        private void ImportAffixPresetDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private bool CanRemoveAffixPresetNameExecute()
        {
            return SelectedAffixPreset != null && !string.IsNullOrWhiteSpace(SelectedAffixPreset.Name);
        }

        private void RemoveAffixPresetNameExecute()
        {
            _dialogCoordinator.ShowMessageAsync(this, $"Delete", $"Are you sure you want to delete preset \"{SelectedAffixPreset.Name}\"", MessageDialogStyle.AffirmativeAndNegative).ContinueWith(t =>
            {
                if (t.Result == MessageDialogResult.Affirmative)
                {
                    _logger.LogInformation($"Deleted preset \"{SelectedAffixPreset.Name}\"");
                    _affixManager.RemoveAffixPreset(SelectedAffixPreset);
                }
            });
        }

        private void RemoveMaxrollBuildExecute(MaxrollBuild maxrollBuild)
        {
            if (maxrollBuild != null)
            {
                _buildsManager.RemoveMaxrollBuild(maxrollBuild.Id);
            }
        }

        private void SelectMaxrollBuildExecute(MaxrollBuild maxrollBuild)
        {
            SelectedMaxrollBuild = maxrollBuild;
        }

        private void UpdateMaxrollBuildExecute(MaxrollBuild build)
        {
            _buildsManager.DownloadMaxrollBuild(build.Id);
        }

        private void WebMaxrollBuildExecute(MaxrollBuild maxrollBuild)
        {
            string uri = @$"https://maxroll.gg/d4/planner/{maxrollBuild.Id}";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void UpdateAffixPresets()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                AffixPresets.Clear();
                AffixPresets.AddRange(_affixManager.AffixPresets);
                if (AffixPresets.Any())
                {
                    SelectedAffixPreset = AffixPresets[0];
                }
            });
        }

        private void UpdateMaxrollBuilds()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                MaxrollBuilds.Clear();
                MaxrollBuilds.AddRange(_buildsManager.MaxrollBuilds);
                SelectedMaxrollBuild = new();
            });
        }

        public void ImportAffixPresetCommandExecute(string fileName)
        {
            string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
            AffixPreset affixPreset;

            try
            {
                if (File.Exists(fileName))
                {
                    using FileStream stream = File.OpenRead(fileName);
                    affixPreset = JsonSerializer.Deserialize<AffixPreset>(stream) ?? new AffixPreset();

                    if (!string.IsNullOrEmpty(affixPreset?.Name))
                    {
                        // Find unique name
                        string name = affixPreset.Name;
                        int counter = 0;

                        while (_affixPresets.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            counter++;
                            name = $"{affixPreset.Name}-{counter}";
                        }

                        affixPreset.Name = name;
                        _affixManager.AddAffixPreset(affixPreset);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"Failed to import {fileName}"
                });
            }
        }

        #endregion
    }
}
