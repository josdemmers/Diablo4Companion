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
using System.Windows.Media;

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
        private Color _colorBuild1 = Colors.Green;
        private Color _colorBuild2 = Colors.Green;
        private Color _colorBuild12 = Colors.Green;
        private AffixPreset _selectedAffixPreset = new AffixPreset();
        private AffixPreset _selectedAffixPresetBuild1 = new AffixPreset();
        private AffixPreset _selectedAffixPresetBuild2 = new AffixPreset();
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
            // Init View commands - Merge
            MergeBuildsCommand = new DelegateCommand(MergeBuildsExecute, CanMergeBuildsExecute);
            SetAffixColorBuild1Command = new DelegateCommand(SetAffixColorBuild1Execute, CanSetAffixColorBuild1Execute);
            SetAffixColorBuild2Command = new DelegateCommand(SetAffixColorBuild2Execute, CanSetAffixColorBuild2Execute);
            SetAffixColorBuild12Command = new DelegateCommand(SetAffixColorBuild12Execute, CanSetAffixColorBuild12Execute);
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
        public DelegateCommand MergeBuildsCommand { get; }
        public DelegateCommand RemoveAffixPresetNameCommand { get; }
        public DelegateCommand<D4BuildsBuild> RemoveD4BuildsBuildCommand { get; }
        public DelegateCommand<MaxrollBuild> RemoveMaxrollBuildCommand { get; }
        public DelegateCommand<D4BuildsBuild> SelectD4BuildsBuildCommand { get; }
        public DelegateCommand<MaxrollBuild> SelectMaxrollBuildCommand { get; }
        public DelegateCommand SetAffixColorBuild1Command { get; }
        public DelegateCommand SetAffixColorBuild2Command { get; }
        public DelegateCommand SetAffixColorBuild12Command { get; }
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

        public bool ChangeColorBuild1 { get; set; } = false;
        public bool ChangeColorBuild2 { get; set; } = false;
        public bool ChangeColorBuild12 { get; set; } = false;

        public Color ColorBuild1
        {
            get => _colorBuild1;
            set
            {
                _colorBuild1 = value;
                RaisePropertyChanged(nameof(ColorBuild1));
            }
        }

        public Color ColorBuild2
        {
            get => _colorBuild2;
            set
            {
                _colorBuild2 = value;
                RaisePropertyChanged(nameof(ColorBuild2));
            }
        }

        public Color ColorBuild12
        {
            get => _colorBuild12;
            set
            {
                _colorBuild12 = value;
                RaisePropertyChanged(nameof(ColorBuild12));
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

        public AffixPreset SelectedAffixPresetBuild1
        {
            get => _selectedAffixPresetBuild1;
            set
            {
                _selectedAffixPresetBuild1 = value;
                if (value == null)
                {
                    _selectedAffixPresetBuild1 = new AffixPreset();
                }
                RaisePropertyChanged(nameof(SelectedAffixPresetBuild1));
                MergeBuildsCommand?.RaiseCanExecuteChanged();
            }
        }

        public AffixPreset SelectedAffixPresetBuild2
        {
            get => _selectedAffixPresetBuild2;
            set
            {
                _selectedAffixPresetBuild2 = value;
                if (value == null)
                {
                    _selectedAffixPresetBuild2 = new AffixPreset();
                }
                RaisePropertyChanged(nameof(SelectedAffixPresetBuild2));
                MergeBuildsCommand?.RaiseCanExecuteChanged();
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

        private bool CanMergeBuildsExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedAffixPresetBuild1.Name)
                && !string.IsNullOrWhiteSpace(SelectedAffixPresetBuild2.Name)
                && !SelectedAffixPresetBuild1.Name.Equals(SelectedAffixPresetBuild2.Name);
        }

        private async void MergeBuildsExecute()
        {
            // Confirm name
            StringWrapper presetName = new StringWrapper
            {
                String = SelectedAffixPresetBuild1.Name
            };

            var setPresetNameDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapConfirmName"] };
            var dataContext = new SetPresetNameViewModel(async instance =>
            {
                await setPresetNameDialog.WaitUntilUnloadedAsync();
            }, presetName);
            setPresetNameDialog.Content = new SetPresetNameView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setPresetNameDialog);
            await setPresetNameDialog.WaitUntilUnloadedAsync();

            // Create new preset
            var buildName = presetName.String;
            buildName = string.IsNullOrWhiteSpace(buildName) ? SelectedAffixPresetBuild1.Name : buildName;
            _affixManager.AffixPresets.RemoveAll(p => p.Name.Equals(buildName));

            var affixPreset = new AffixPreset
            {
                Name = buildName
            };

            // Build 1
            affixPreset.ItemAffixes.AddRange(SelectedAffixPresetBuild1.ItemAffixes.Select(a =>
            {
                return new ItemAffix
                {
                    Id = a.Id,
                    Type = a.Type,
                    Color = ChangeColorBuild1 ? ColorBuild1 : a.Color
                };
            }));
            affixPreset.ItemAspects.AddRange(SelectedAffixPresetBuild1.ItemAspects.Select(a =>
            {
                return new ItemAffix
                {
                    Id = a.Id,
                    Type = a.Type,
                    Color = ChangeColorBuild1 ? ColorBuild1 : a.Color
                };
            }));
            affixPreset.ItemSigils.AddRange(SelectedAffixPresetBuild1.ItemSigils.Select(s =>
            {
                return new ItemAffix
                {
                    Id = s.Id,
                    Type = s.Type,
                    Color = ChangeColorBuild1 ? ColorBuild1 : s.Color
                };
            }));

            // Build 2
            foreach (var itemAffixBuild2 in SelectedAffixPresetBuild2.ItemAffixes)
            {
                var itemAffix = affixPreset.ItemAffixes.FirstOrDefault(a => a.Id.Equals(itemAffixBuild2.Id) && a.Type.Equals(itemAffixBuild2.Type));
                if (itemAffix == null)
                {
                    affixPreset.ItemAffixes.Add(new ItemAffix
                    {
                        Id = itemAffixBuild2.Id,
                        Type = itemAffixBuild2.Type,
                        Color = ChangeColorBuild2 ? ColorBuild2 : itemAffixBuild2.Color,
                    });
                }
                else
                {
                    itemAffix.Color = ChangeColorBuild12 ? ColorBuild12 : itemAffix.Color;
                }
            }
            foreach (var itemAspectBuild2 in SelectedAffixPresetBuild2.ItemAspects)
            {
                var itemAspect = affixPreset.ItemAspects.FirstOrDefault(a => a.Id.Equals(itemAspectBuild2.Id) && a.Type.Equals(itemAspectBuild2.Type));
                if (itemAspect == null)
                {
                    affixPreset.ItemAspects.Add(new ItemAffix
                    {
                        Id = itemAspectBuild2.Id,
                        Type = itemAspectBuild2.Type,
                        Color = ChangeColorBuild2 ? ColorBuild2 : itemAspectBuild2.Color,
                    });
                }
                else
                {
                    itemAspect.Color = ChangeColorBuild12 ? ColorBuild12 : itemAspect.Color;
                }
            }
            foreach (var itemSigilsBuild2 in SelectedAffixPresetBuild2.ItemSigils)
            {
                var itemSigil = affixPreset.ItemSigils.FirstOrDefault(a => a.Id.Equals(itemSigilsBuild2.Id) && a.Type.Equals(itemSigilsBuild2.Type));
                if (itemSigil == null)
                {
                    affixPreset.ItemSigils.Add(new ItemAffix
                    {
                        Id = itemSigilsBuild2.Id,
                        Type = itemSigilsBuild2.Type,
                        Color = ChangeColorBuild2 ? ColorBuild2 : itemSigilsBuild2.Color,
                    });
                }
                else
                {
                    itemSigil.Color = ChangeColorBuild12 ? ColorBuild12 : itemSigil.Color;
                }
            }

            // Add merged build
            _affixManager.AddAffixPreset(affixPreset);
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

        private bool CanSetAffixColorBuild1Execute()
        {
            return ChangeColorBuild1;
        }

        private async void SetAffixColorBuild1Execute()
        {
            ColorWrapper colorWrapper = new ColorWrapper
            {
                Color = ColorBuild1
            };

            var setAffixColorDialog = new CustomDialog() { Title = "Set affix color" };
            var dataContext = new SelectAffixColorViewModel(async instance =>
            {
                await setAffixColorDialog.WaitUntilUnloadedAsync();
            }, colorWrapper);
            setAffixColorDialog.Content = new SetAffixColorView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixColorDialog);
            await setAffixColorDialog.WaitUntilUnloadedAsync();
            ColorBuild1 = colorWrapper.Color;
        }

        private bool CanSetAffixColorBuild2Execute()
        {
            return ChangeColorBuild2;
        }

        private async void SetAffixColorBuild2Execute()
        {
            ColorWrapper colorWrapper = new ColorWrapper
            {
                Color = ColorBuild1
            };

            var setAffixColorDialog = new CustomDialog() { Title = "Set affix color" };
            var dataContext = new SelectAffixColorViewModel(async instance =>
            {
                await setAffixColorDialog.WaitUntilUnloadedAsync();
            }, colorWrapper);
            setAffixColorDialog.Content = new SetAffixColorView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixColorDialog);
            await setAffixColorDialog.WaitUntilUnloadedAsync();
            ColorBuild2 = colorWrapper.Color;
        }

        private bool CanSetAffixColorBuild12Execute()
        {
            return ChangeColorBuild12;
        }

        private async void SetAffixColorBuild12Execute()
        {
            ColorWrapper colorWrapper = new ColorWrapper
            {
                Color = ColorBuild12
            };

            var setAffixColorDialog = new CustomDialog() { Title = "Set affix color" };
            var dataContext = new SelectAffixColorViewModel(async instance =>
            {
                await setAffixColorDialog.WaitUntilUnloadedAsync();
            }, colorWrapper);
            setAffixColorDialog.Content = new SetAffixColorView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixColorDialog);
            await setAffixColorDialog.WaitUntilUnloadedAsync();
            ColorBuild12 = colorWrapper.Color;
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
