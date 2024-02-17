using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.Localization;
using D4Companion.Views.Dialogs;
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
using System.Threading.Tasks;
using System.Windows;

namespace D4Companion.ViewModels.Dialogs
{
    public class ImportAffixPresetViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly IBuildsManager _buildsManager;
        private readonly IBuildsManagerD4Builds _buildsManagerD4Builds;
        private readonly IDialogCoordinator _dialogCoordinator;

        private ObservableCollection<AffixPreset> _affixPresets = new();
        private ObservableCollection<D4BuildsBuild> _d4BuildsBuilds = new();
        private ObservableCollection<MaxrollBuild> _maxrollBuilds = new();

        private string _buildId = string.Empty;
        private string _buildIdD4Builds = string.Empty;
        private AffixPreset _selectedAffixPreset = new AffixPreset();
        private D4BuildsBuild _selectedD4BuildsBuild = new();
        private MaxrollBuild _selectedMaxrollBuild = new();

        // Start of Constructors region

        #region Constructors

        public ImportAffixPresetViewModel(Action<ImportAffixPresetViewModel> closeHandler, IAffixManager affixManager, IBuildsManager buildsManager, IBuildsManagerD4Builds buildsManagerD4Builds)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
            _eventAggregator.GetEvent<AffixPresetAddedEvent>().Subscribe(HandleAffixPresetAddedEvent);
            _eventAggregator.GetEvent<AffixPresetRemovedEvent>().Subscribe(HandleAffixPresetRemovedEvent);
            _eventAggregator.GetEvent<D4BuildsBuildsLoadedEvent>().Subscribe(HandleD4BuildsBuildsLoadedEvent);
            _eventAggregator.GetEvent<MaxrollBuildsLoadedEvent>().Subscribe(HandleMaxrollBuildsLoadedEvent);

            // Init services
            _logger = (ILogger<ImportAffixPresetViewModel>)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ILogger<ImportAffixPresetViewModel>));
            _affixManager = affixManager;
            _buildsManager = buildsManager;
            _buildsManagerD4Builds = buildsManagerD4Builds;
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));

            // Init View commands
            CloseCommand = new DelegateCommand<ImportAffixPresetViewModel>(closeHandler);
            ImportAffixPresetDoneCommand = new DelegateCommand(ImportAffixPresetDoneExecute);
            RemoveAffixPresetNameCommand = new DelegateCommand(RemoveAffixPresetNameExecute, CanRemoveAffixPresetNameExecute);
            // Init View commands - D4Builds
            AddD4BuildsBuildCommand = new DelegateCommand(AddD4BuildsBuildExecute, CanAddD4BuildsBuildExecute);
            AddD4BuildsBuildAsPresetCommand = new DelegateCommand<D4BuildsBuildVariant>(AddD4BuildsBuildAsPresetExecute);
            RemoveD4BuildsBuildCommand = new DelegateCommand<D4BuildsBuild>(RemoveD4BuildsBuildExecute);
            SelectD4BuildsBuildCommand = new DelegateCommand<D4BuildsBuild>(SelectD4BuildsBuildExecute);
            UpdateD4BuildsBuildCommand = new DelegateCommand<D4BuildsBuild>(UpdateD4BuildsBuildExecute);
            VisitD4BuildsCommand = new DelegateCommand(VisitD4BuildsExecute);
            WebD4BuildsBuildCommand = new DelegateCommand<D4BuildsBuild>(WebD4BuildsBuildExecute);
            // Init View commands - Maxroll
            AddMaxrollBuildCommand = new DelegateCommand(AddMaxrollBuildExecute, CanAddMaxrollBuildExecute);
            AddMaxrollBuildAsPresetCommand = new DelegateCommand<MaxrollBuildDataProfileJson>(AddMaxrollBuildAsPresetExecute);
            RemoveMaxrollBuildCommand = new DelegateCommand<MaxrollBuild>(RemoveMaxrollBuildExecute);
            SelectMaxrollBuildCommand = new DelegateCommand<MaxrollBuild>(SelectMaxrollBuildExecute);
            UpdateMaxrollBuildCommand = new DelegateCommand<MaxrollBuild>(UpdateMaxrollBuildExecute);
            VisitMaxrollCommand = new DelegateCommand(VisitMaxrollExecute);
            WebMaxrollBuildCommand = new DelegateCommand<MaxrollBuild>(WebMaxrollBuildExecute);

            // Load affix presets
            UpdateAffixPresets();

            // Load builds
            UpdateD4BuildsBuilds();
            UpdateMaxrollBuilds();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixPreset> AffixPresets { get => _affixPresets; set => _affixPresets = value; }
        public ObservableCollection<D4BuildsBuild> D4BuildsBuilds { get => _d4BuildsBuilds; set => _d4BuildsBuilds = value; }
        public ObservableCollection<MaxrollBuild> MaxrollBuilds { get => _maxrollBuilds; set => _maxrollBuilds = value; }

        public DelegateCommand AddD4BuildsBuildCommand { get; }
        public DelegateCommand AddMaxrollBuildCommand { get; }
        public DelegateCommand<D4BuildsBuildVariant> AddD4BuildsBuildAsPresetCommand { get; }
        public DelegateCommand<MaxrollBuildDataProfileJson> AddMaxrollBuildAsPresetCommand { get; }
        public DelegateCommand<ImportAffixPresetViewModel> CloseCommand { get; }
        public DelegateCommand ImportAffixPresetDoneCommand { get; }
        public DelegateCommand RemoveAffixPresetNameCommand { get; }
        public DelegateCommand<D4BuildsBuild> RemoveD4BuildsBuildCommand { get; }
        public DelegateCommand<MaxrollBuild> RemoveMaxrollBuildCommand { get; }
        public DelegateCommand<D4BuildsBuild> SelectD4BuildsBuildCommand { get; }
        public DelegateCommand<MaxrollBuild> SelectMaxrollBuildCommand { get; }
        public DelegateCommand<D4BuildsBuild> UpdateD4BuildsBuildCommand { get; }
        public DelegateCommand<MaxrollBuild> UpdateMaxrollBuildCommand { get; }
        public DelegateCommand VisitD4BuildsCommand { get; }
        public DelegateCommand VisitMaxrollCommand { get; }
        public DelegateCommand<D4BuildsBuild> WebD4BuildsBuildCommand { get; }
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

        public string BuildIdD4Builds
        {
            get => _buildIdD4Builds;
            set
            {
                _buildIdD4Builds = value;
                RaisePropertyChanged(nameof(BuildIdD4Builds));
                AddD4BuildsBuildCommand?.RaiseCanExecuteChanged();
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

        public D4BuildsBuild SelectedD4BuildsBuild
        {
            get => _selectedD4BuildsBuild;
            set
            {
                _selectedD4BuildsBuild = value;
                if (value == null)
                {
                    _selectedD4BuildsBuild = new();
                }
                RaisePropertyChanged(nameof(SelectedD4BuildsBuild));
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

        private bool CanAddD4BuildsBuildExecute()
        {
            return !string.IsNullOrWhiteSpace(BuildIdD4Builds) && BuildIdD4Builds.Length == 36;
        }

        private async void AddD4BuildsBuildExecute()
        {
            _ = Task.Factory.StartNew(() =>
            {
                _buildsManagerD4Builds.DownloadD4BuildsBuild(BuildIdD4Builds);
            });

            var d4BuildsDownloadDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapDownloadingWait"] };
            var dataContext = new D4BuildsDownloadViewModel(async instance =>
            {
                await d4BuildsDownloadDialog.WaitUntilUnloadedAsync();
            });
            d4BuildsDownloadDialog.Content = new D4BuildsDownloadView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, d4BuildsDownloadDialog);
            await d4BuildsDownloadDialog.WaitUntilUnloadedAsync();
        }

        private bool CanAddMaxrollBuildExecute()
        {
            return !string.IsNullOrWhiteSpace(BuildId) && BuildId.Length == 8 && !BuildId.Contains("#");
        }

        private void AddMaxrollBuildExecute()
        {
            _buildsManager.DownloadMaxrollBuild(BuildId);
        }

        private async void AddD4BuildsBuildAsPresetExecute(D4BuildsBuildVariant d4BuildsBuildVariant)
        {
            // Show dialog to modify preset name
            StringWrapper presetName = new StringWrapper
            {
                String = SelectedD4BuildsBuild.Name
            };

            var setPresetNameDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapConfirmName"] };
            var dataContext = new SetPresetNameViewModel(async instance =>
            {
                await setPresetNameDialog.WaitUntilUnloadedAsync();
            }, presetName);
            setPresetNameDialog.Content = new SetPresetNameView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setPresetNameDialog);
            await setPresetNameDialog.WaitUntilUnloadedAsync();

            // Add confirmed preset name.
            _buildsManagerD4Builds.CreatePresetFromD4BuildsBuild(d4BuildsBuildVariant, SelectedD4BuildsBuild.Name, presetName.String);
        }

        private async void AddMaxrollBuildAsPresetExecute(MaxrollBuildDataProfileJson maxrollBuildDataProfileJson)
        {
            // Show dialog to modify preset name
            StringWrapper presetName = new StringWrapper
            {
                String = SelectedMaxrollBuild.Name
            };
            
            var setPresetNameDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapConfirmName"] };
            var dataContext = new SetPresetNameViewModel(async instance =>
            {
                await setPresetNameDialog.WaitUntilUnloadedAsync();
            }, presetName);
            setPresetNameDialog.Content = new SetPresetNameView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setPresetNameDialog);
            await setPresetNameDialog.WaitUntilUnloadedAsync();

            // Add confirmed preset name.
            _buildsManager.CreatePresetFromMaxrollBuild(SelectedMaxrollBuild, maxrollBuildDataProfileJson.Name, presetName.String);
        }

        private void HandleAffixPresetAddedEvent()
        {
            UpdateAffixPresets();

            string presetName = !string.IsNullOrWhiteSpace(SelectedMaxrollBuild?.Name) ? SelectedMaxrollBuild.Name : 
                !string.IsNullOrWhiteSpace(SelectedD4BuildsBuild.Name) ? SelectedD4BuildsBuild.Name : string.Empty;
            if (string.IsNullOrWhiteSpace(presetName)) return;

            // Select added preset
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(presetName));
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

        private void HandleD4BuildsBuildsLoadedEvent()
        {
            UpdateD4BuildsBuilds();
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

        private void RemoveD4BuildsBuildExecute(D4BuildsBuild d4BuildsBuild)
        {
            if (d4BuildsBuild != null)
            {
                _buildsManagerD4Builds.RemoveD4BuildsBuild(d4BuildsBuild.Id);
            }
        }

        private void RemoveMaxrollBuildExecute(MaxrollBuild maxrollBuild)
        {
            if (maxrollBuild != null)
            {
                _buildsManager.RemoveMaxrollBuild(maxrollBuild.Id);
            }
        }

        private void SelectD4BuildsBuildExecute(D4BuildsBuild d4BuildsBuild)
        {
            SelectedD4BuildsBuild = d4BuildsBuild;
        }

        private void SelectMaxrollBuildExecute(MaxrollBuild maxrollBuild)
        {
            SelectedMaxrollBuild = maxrollBuild;
        }

        private async void UpdateD4BuildsBuildExecute(D4BuildsBuild build)
        {
            _ = Task.Factory.StartNew(() =>
            {
                _buildsManagerD4Builds.DownloadD4BuildsBuild(build.Id);
            });

            var d4BuildsDownloadDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapDownloadingWait"] };
            var dataContext = new D4BuildsDownloadViewModel(async instance =>
            {
                await d4BuildsDownloadDialog.WaitUntilUnloadedAsync();
            });
            d4BuildsDownloadDialog.Content = new D4BuildsDownloadView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, d4BuildsDownloadDialog);
            await d4BuildsDownloadDialog.WaitUntilUnloadedAsync();
        }

        private void UpdateMaxrollBuildExecute(MaxrollBuild build)
        {
            _buildsManager.DownloadMaxrollBuild(build.Id);
        }

        private void VisitD4BuildsExecute()
        {
            string uri = @"https://d4builds.gg/";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        private void VisitMaxrollExecute()
        {
            string uri = @"https://maxroll.gg/d4/build-guides";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        private void WebD4BuildsBuildExecute(D4BuildsBuild d4BuildsBuild)
        {
            string uri = @$"https://d4builds.gg/builds/{d4BuildsBuild.Id}";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
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

        private void UpdateD4BuildsBuilds()
        {
            Application.Current?.Dispatcher.Invoke((Delegate)(() =>
            {
                D4BuildsBuilds.Clear();
                D4BuildsBuilds.AddRange(_buildsManagerD4Builds.D4BuildsBuilds);
                SelectedD4BuildsBuild = new();
            }));
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
