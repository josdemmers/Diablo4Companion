using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Extensions;
using D4Companion.Interfaces;
using D4Companion.Localization;
using D4Companion.Messages;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace D4Companion.ViewModels.Dialogs
{
    public class ImportAffixPresetViewModel : ObservableObject,
        IDisposable,
        IRecipient<AffixPresetAddedMessage>,
        IRecipient<AffixPresetRemovedMessage>,
        IRecipient<D2CoreBuildsLoadedMessage>,
        IRecipient<D4BuildsBuildsLoadedMessage>,
        IRecipient<MaxrollBuildsLoadedMessage>,
        IRecipient<MobalyticsBuildsLoadedMessage>,
        IRecipient<MobalyticsProfilesLoadedMessage>
    {
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly IBuildsManagerD2Core _buildsManagerD2Core;
        private readonly IBuildsManagerD4Builds _buildsManagerD4Builds;
        private readonly IBuildsManagerMaxroll _buildsManagerMaxroll;
        private readonly IBuildsManagerMobalytics _buildsManagerMobalytics;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        private ObservableCollection<AffixPreset> _affixPresets = new();
        private ObservableCollection<D2CoreBuild> _d2CoreBuilds = new();
        private ObservableCollection<D4BuildsBuild> _d4BuildsBuilds = new();
        private ObservableCollection<MaxrollBuild> _maxrollBuilds = new();
        private ObservableCollection<MobalyticsBuild> _mobalyticsBuilds = new();
        private ObservableCollection<MobalyticsProfile> _mobalyticsProfiles = new();

        private string _buildIdD2Core = string.Empty;
        private string _buildIdorUrlD2Core = string.Empty;
        private string _buildIdD4Builds = string.Empty;
        private string _buildIdorUrlD4Builds = string.Empty;
        private string _buildIdMaxroll = string.Empty;
        private string _buildIdorUrlMaxroll = string.Empty;
        private string _buildIdMobalytics = string.Empty;
        private Color _colorBuild1 = Colors.Green;
        private Color _colorBuild2 = Colors.Green;
        private Color _colorBuild12 = Colors.Green;
        private AffixPreset _selectedAffixPreset = new AffixPreset();
        private AffixPreset _selectedAffixPresetBuild1 = new AffixPreset();
        private AffixPreset _selectedAffixPresetBuild2 = new AffixPreset();
        private D2CoreBuild _selectedD2CoreBuild = new();
        private D4BuildsBuild _selectedD4BuildsBuild = new();
        private MaxrollBuild _selectedMaxrollBuild = new();
        private MobalyticsBuild _selectedMobalyticsBuild = new();
        private MobalyticsProfile _selectedMobalyticsProfile = new();
        private int _selectedTabIndex = 0;
        private int _selectedTabIndexMobalytics = 0;

        // Start of Constructors region

        #region Constructors

        public ImportAffixPresetViewModel(Action<ImportAffixPresetViewModel?> closeHandler, IAffixManager affixManager,
            IBuildsManagerD2Core buildsManagerD2Core, IBuildsManagerD4Builds buildsManagerD4Builds, IBuildsManagerMaxroll buildsManagerMaxroll, IBuildsManagerMobalytics buildsManagerMobalytics, 
            ISettingsManager settingsManager, BuildImportWebsite selectedBuildImportWebsite)
        {
            // Init services
            _affixManager = affixManager;
            _buildsManagerD2Core = buildsManagerD2Core;
            _buildsManagerD4Builds = buildsManagerD4Builds;
            _buildsManagerMaxroll = buildsManagerMaxroll;
            _buildsManagerMobalytics = buildsManagerMobalytics;
            _dialogCoordinator = App.Current.Services.GetRequiredService<IDialogCoordinator>();
            _logger = App.Current.Services.GetRequiredService<ILogger<ImportAffixPresetViewModel>>();
            _settingsManager = settingsManager;

            // Init messages
            WeakReferenceMessenger.Default.RegisterAll(this);

            // Init view commands
            CloseCommand = new RelayCommand<ImportAffixPresetViewModel>(closeHandler);
            ExportAffixPresetCommand = new RelayCommand(ExportAffixPresetCommandExecute, CanExportAffixPresetCommandExecute);
            ImportAffixPresetDoneCommand = new RelayCommand(ImportAffixPresetDoneExecute);
            RemoveAffixPresetNameCommand = new RelayCommand(RemoveAffixPresetNameExecute, CanRemoveAffixPresetNameExecute);
            // Init view commands - Merge
            MergeBuildsCommand = new RelayCommand(MergeBuildsExecute, CanMergeBuildsExecute);
            SetAffixColorBuild1Command = new RelayCommand(SetAffixColorBuild1Execute, CanSetAffixColorBuild1Execute);
            SetAffixColorBuild2Command = new RelayCommand(SetAffixColorBuild2Execute, CanSetAffixColorBuild2Execute);
            SetAffixColorBuild12Command = new RelayCommand(SetAffixColorBuild12Execute, CanSetAffixColorBuild12Execute);
            // Init view commands - D2Core
            AddD2CoreBuildCommand = new RelayCommand(AddD2CoreBuildExecute, CanAddD2CoreBuildExecute);
            AddD2CoreBuildAsPresetCommand = new RelayCommand<D2CoreBuildDataVariantJson>(AddD2CoreBuildAsPresetExecute);
            RemoveD2CoreBuildCommand = new RelayCommand<D2CoreBuild>(RemoveD2CoreBuildExecute);
            SelectD2CoreBuildCommand = new RelayCommand<D2CoreBuild>(SelectD2CoreBuildExecute);
            UpdateD2CoreBuildCommand = new RelayCommand<D2CoreBuild>(UpdateD2CoreBuildExecute);
            VisitD2CoreCommand = new RelayCommand(VisitD2CoreExecute);
            WebD2CoreBuildCommand = new RelayCommand<D2CoreBuild>(WebD2CoreBuildExecute);
            // Init view commands - D4Builds
            AddD4BuildsBuildCommand = new RelayCommand(AddD4BuildsBuildExecute, CanAddD4BuildsBuildExecute);
            AddD4BuildsBuildAsPresetCommand = new RelayCommand<D4BuildsBuildVariant>(AddD4BuildsBuildAsPresetExecute);
            RemoveD4BuildsBuildCommand = new RelayCommand<D4BuildsBuild>(RemoveD4BuildsBuildExecute);
            SelectD4BuildsBuildCommand = new RelayCommand<D4BuildsBuild>(SelectD4BuildsBuildExecute);
            UpdateD4BuildsBuildCommand = new RelayCommand<D4BuildsBuild>(UpdateD4BuildsBuildExecute);
            VisitD4BuildsCommand = new RelayCommand(VisitD4BuildsExecute);
            WebD4BuildsBuildCommand = new RelayCommand<D4BuildsBuild>(WebD4BuildsBuildExecute);
            // Init view commands - Maxroll
            AddMaxrollBuildCommand = new RelayCommand(AddMaxrollBuildExecute, CanAddMaxrollBuildExecute);
            AddMaxrollBuildAsPresetCommand = new RelayCommand<MaxrollBuildDataProfileJson>(AddMaxrollBuildAsPresetExecute);
            RemoveMaxrollBuildCommand = new RelayCommand<MaxrollBuild>(RemoveMaxrollBuildExecute);
            SelectMaxrollBuildCommand = new RelayCommand<MaxrollBuild>(SelectMaxrollBuildExecute);
            UpdateMaxrollBuildCommand = new RelayCommand<MaxrollBuild>(UpdateMaxrollBuildExecute);
            VisitMaxrollCommand = new RelayCommand(VisitMaxrollExecute);
            WebMaxrollBuildCommand = new RelayCommand<MaxrollBuild>(WebMaxrollBuildExecute);
            // Init view commands - Mobalytics
            AddMobalyticsBuildCommand = new RelayCommand(AddMobalyticsBuildExecute, CanAddMobalyticsBuildExecute);
            AddMobalyticsBuildAsPresetCommand = new RelayCommand<MobalyticsBuildVariant>(AddMobalyticsBuildAsPresetExecute);
            AddMobalyticsProfileBuildVariantCommand = new RelayCommand<MobalyticsProfileBuildVariant>(AddMobalyticsProfileBuildVariantExecute);
            RemoveMobalyticsBuildCommand = new RelayCommand<MobalyticsBuild>(RemoveMobalyticsBuildExecute);
            RemoveMobalyticsProfileCommand = new RelayCommand<MobalyticsProfile>(RemoveMobalyticsProfileExecute);
            SelectMobalyticsBuildCommand = new RelayCommand<MobalyticsBuild>(SelectMobalyticsBuildExecute);
            SelectMobalyticsProfileCommand = new RelayCommand<MobalyticsProfile>(SelectMobalyticsProfileExecute);
            UpdateMobalyticsBuildCommand = new RelayCommand<MobalyticsBuild>(UpdateMobalyticsBuildExecute);
            UpdateMobalyticsProfileCommand = new RelayCommand<MobalyticsProfile>(UpdateMobalyticsProfileExecute);
            VisitMobalyticsCommand = new RelayCommand(VisitMobalyticsExecute);
            WebMobalyticsBuildCommand = new RelayCommand<MobalyticsBuild>(WebMobalyticsBuildExecute);
            WebMobalyticsProfileCommand = new RelayCommand<MobalyticsProfile>(WebMobalyticsProfileExecute);
            // Init default colors
            ColorBuild1 = _settingsManager.Settings.DefaultColorNormal;
            ColorBuild2 = _settingsManager.Settings.DefaultColorNormal;
            ColorBuild12 = _settingsManager.Settings.DefaultColorNormal;

            // Load affix presets
            UpdateAffixPresets();

            // Load builds and profiles
            UpdateD2CoreBuilds();
            UpdateD4BuildsBuilds();
            UpdateMaxrollBuilds();
            UpdateMobalyticsBuilds();
            UpdateMobalyticsProfiles();

            // Select correct website tab
            SelectedTabIndex = selectedBuildImportWebsite.Name.Equals("D2Core.com") ? 2
                : selectedBuildImportWebsite.Name.Equals("D4Builds.gg") ? 3
                : selectedBuildImportWebsite.Name.Equals("Maxroll.gg") ? 4
                : selectedBuildImportWebsite.Name.Equals("Mobalytics.gg") ? 5
                : 0;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixPreset> AffixPresets { get => _affixPresets; set => _affixPresets = value; }
        public ObservableCollection<D2CoreBuild> D2CoreBuilds { get => _d2CoreBuilds; set => _d2CoreBuilds = value; }
        public ObservableCollection<D4BuildsBuild> D4BuildsBuilds { get => _d4BuildsBuilds; set => _d4BuildsBuilds = value; }
        public ObservableCollection<MaxrollBuild> MaxrollBuilds { get => _maxrollBuilds; set => _maxrollBuilds = value; }
        public ObservableCollection<MobalyticsBuild> MobalyticsBuilds { get => _mobalyticsBuilds; set => _mobalyticsBuilds = value; }
        public ObservableCollection<MobalyticsProfile> MobalyticsProfiles { get => _mobalyticsProfiles; set => _mobalyticsProfiles = value; }

        public ICommand AddD2CoreBuildCommand { get; }
        public ICommand AddD4BuildsBuildCommand { get; }
        public ICommand AddMaxrollBuildCommand { get; }
        public ICommand AddMobalyticsBuildCommand { get; }
        public ICommand AddD2CoreBuildAsPresetCommand { get; }
        public ICommand AddD4BuildsBuildAsPresetCommand { get; }
        public ICommand AddMaxrollBuildAsPresetCommand { get; }
        public ICommand AddMobalyticsBuildAsPresetCommand { get; }
        public ICommand AddMobalyticsProfileBuildVariantCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand ExportAffixPresetCommand { get; }
        public ICommand ImportAffixPresetDoneCommand { get; }
        public ICommand MergeBuildsCommand { get; }
        public ICommand RemoveAffixPresetNameCommand { get; }
        public ICommand RemoveD2CoreBuildCommand { get; }
        public ICommand RemoveD4BuildsBuildCommand { get; }
        public ICommand RemoveMaxrollBuildCommand { get; }
        public ICommand RemoveMobalyticsBuildCommand { get; }
        public ICommand RemoveMobalyticsProfileCommand { get; }
        public ICommand SelectD2CoreBuildCommand { get; }
        public ICommand SelectD4BuildsBuildCommand { get; }
        public ICommand SelectMaxrollBuildCommand { get; }
        public ICommand SelectMobalyticsBuildCommand { get; }
        public ICommand SelectMobalyticsProfileCommand { get; }
        public ICommand SetAffixColorBuild1Command { get; }
        public ICommand SetAffixColorBuild2Command { get; }
        public ICommand SetAffixColorBuild12Command { get; }
        public ICommand UpdateD2CoreBuildCommand { get; }
        public ICommand UpdateD4BuildsBuildCommand { get; }
        public ICommand UpdateMaxrollBuildCommand { get; }
        public ICommand UpdateMobalyticsBuildCommand { get; }
        public ICommand UpdateMobalyticsProfileCommand { get; }
        public ICommand VisitD2CoreCommand { get; }
        public ICommand VisitD4BuildsCommand { get; }
        public ICommand VisitMaxrollCommand { get; }
        public ICommand VisitMobalyticsCommand { get; }
        public ICommand WebD2CoreBuildCommand { get; }
        public ICommand WebD4BuildsBuildCommand { get; }
        public ICommand WebMaxrollBuildCommand { get; }
        public ICommand WebMobalyticsBuildCommand { get; }
        public ICommand WebMobalyticsProfileCommand { get; }

        public string BuildIdMaxroll
        {
            get => _buildIdMaxroll;
            set
            {
                _buildIdMaxroll = value;
                OnPropertyChanged(nameof(BuildIdMaxroll));
            }
        }

        public string BuildIdorUrlMaxroll
        {
            get => _buildIdorUrlMaxroll;
            set
            {
                _buildIdorUrlMaxroll = value;
                OnPropertyChanged(nameof(BuildIdorUrlMaxroll));
                ((RelayCommand)AddMaxrollBuildCommand).NotifyCanExecuteChanged();
            }
        }

        public string BuildIdD2Core
        {
            get => _buildIdD2Core;
            set
            {
                _buildIdD2Core = value;
                OnPropertyChanged(nameof(BuildIdD2Core));
            }
        }

        public string BuildIdorUrlD2Core
        {
            get => _buildIdorUrlD2Core;
            set
            {
                _buildIdorUrlD2Core = value;
                OnPropertyChanged(nameof(BuildIdorUrlD2Core));
                ((RelayCommand)AddD2CoreBuildCommand).NotifyCanExecuteChanged();
            }
        }

        public string BuildIdD4Builds
        {
            get => _buildIdD4Builds;
            set
            {
                _buildIdD4Builds = value;
                OnPropertyChanged(nameof(BuildIdD4Builds));
            }
        }

        public string BuildIdorUrlD4Builds
        {
            get => _buildIdorUrlD4Builds;
            set
            {
                _buildIdorUrlD4Builds = value;
                OnPropertyChanged(nameof(BuildIdorUrlD4Builds));
                ((RelayCommand)AddD4BuildsBuildCommand).NotifyCanExecuteChanged();
            }
        }

        public string BuildIdMobalytics
        {
            get => _buildIdMobalytics;
            set
            {
                _buildIdMobalytics = value;
                if (_buildIdMobalytics.Contains("profile") && _buildIdMobalytics.Contains("builds"))
                {
                    _buildIdMobalytics = _buildIdMobalytics.Split('?')[0];
                }

                OnPropertyChanged(nameof(BuildIdMobalytics));
                OnPropertyChanged(nameof(UrlValidStatusMobalytics));
                ((RelayCommand)AddMobalyticsBuildCommand).NotifyCanExecuteChanged();
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
                OnPropertyChanged(nameof(ColorBuild1));
            }
        }

        public Color ColorBuild2
        {
            get => _colorBuild2;
            set
            {
                _colorBuild2 = value;
                OnPropertyChanged(nameof(ColorBuild2));
            }
        }

        public Color ColorBuild12
        {
            get => _colorBuild12;
            set
            {
                _colorBuild12 = value;
                OnPropertyChanged(nameof(ColorBuild12));
            }
        }

        public bool IsImportParagonD2CoreEnabled
        {
            get => _settingsManager.Settings.IsImportParagonD2CoreEnabled;
            set
            {
                _settingsManager.Settings.IsImportParagonD2CoreEnabled = value;
                OnPropertyChanged(nameof(IsImportParagonD2CoreEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsImportParagonD4BuildsEnabled
        {
            get => _settingsManager.Settings.IsImportParagonD4BuildsEnabled;
            set
            {
                _settingsManager.Settings.IsImportParagonD4BuildsEnabled = value;
                OnPropertyChanged(nameof(IsImportParagonD4BuildsEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsImportParagonMaxrollEnabled
        {
            get => _settingsManager.Settings.IsImportParagonMaxrollEnabled;
            set
            {
                _settingsManager.Settings.IsImportParagonMaxrollEnabled = value;
                OnPropertyChanged(nameof(IsImportParagonMaxrollEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsImportParagonMobalyticsEnabled
        {
            get => _settingsManager.Settings.IsImportParagonMobalyticsEnabled;
            set
            {
                _settingsManager.Settings.IsImportParagonMobalyticsEnabled = value;
                OnPropertyChanged(nameof(IsImportParagonMobalyticsEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsImportUniqueAffixesD2CoreEnabled
        {
            get => _settingsManager.Settings.IsImportUniqueAffixesD2CoreEnabled;
            set
            {
                _settingsManager.Settings.IsImportUniqueAffixesD2CoreEnabled = value;
                OnPropertyChanged(nameof(IsImportUniqueAffixesD2CoreEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsImportUniqueAffixesMaxrollEnabled
        {
            get => _settingsManager.Settings.IsImportUniqueAffixesMaxrollEnabled;
            set
            {
                _settingsManager.Settings.IsImportUniqueAffixesMaxrollEnabled = value;
                OnPropertyChanged(nameof(IsImportUniqueAffixesMaxrollEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsImportUniqueAffixesMobalyticsEnabled
        {
            get => _settingsManager.Settings.IsImportUniqueAffixesMobalyticsEnabled;
            set
            {
                _settingsManager.Settings.IsImportUniqueAffixesMobalyticsEnabled = value;
                OnPropertyChanged(nameof(IsImportUniqueAffixesMobalyticsEnabled));

                _settingsManager.SaveSettings();
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
                OnPropertyChanged(nameof(SelectedAffixPreset));

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ((RelayCommand)ExportAffixPresetCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)RemoveAffixPresetNameCommand).NotifyCanExecuteChanged();
                });
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
                OnPropertyChanged(nameof(SelectedAffixPresetBuild1));
                ((RelayCommand)MergeBuildsCommand).NotifyCanExecuteChanged();
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
                OnPropertyChanged(nameof(SelectedAffixPresetBuild2));
                ((RelayCommand)MergeBuildsCommand).NotifyCanExecuteChanged();
            }
        }

        public D2CoreBuild SelectedD2CoreBuild
        {
            get => _selectedD2CoreBuild;
            set
            {
                _selectedD2CoreBuild = value;
                if (value == null)
                {
                    _selectedD2CoreBuild = new();
                }
                OnPropertyChanged(nameof(SelectedD2CoreBuild));
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
                OnPropertyChanged(nameof(SelectedD4BuildsBuild));
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
                OnPropertyChanged(nameof(SelectedMaxrollBuild));
            }
        }

        public MobalyticsBuild SelectedMobalyticsBuild
        {
            get => _selectedMobalyticsBuild;
            set
            {
                _selectedMobalyticsBuild = value;
                if (value == null)
                {
                    _selectedMobalyticsBuild = new();
                }
                OnPropertyChanged(nameof(SelectedMobalyticsBuild));
            }
        }

        public MobalyticsProfile SelectedMobalyticsProfile
        {
            get => _selectedMobalyticsProfile;
            set
            {
                _selectedMobalyticsProfile = value;
                if (value == null)
                {
                    _selectedMobalyticsProfile = new();
                }
                OnPropertyChanged(nameof(SelectedMobalyticsProfile));
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        public int SelectedTabIndexMobalytics
        {
            get => _selectedTabIndexMobalytics;
            set
            {
                _selectedTabIndexMobalytics = value;
                OnPropertyChanged();
            }
        }

        public string UrlValidStatusMobalytics
        {
            get
            {
                string urlValidStatusMobalytics = string.Empty;

                if (BuildIdMobalytics.Contains("mobalytics.gg", StringComparison.OrdinalIgnoreCase) &&
                   BuildIdMobalytics.Contains("profile", StringComparison.OrdinalIgnoreCase) &&
                   BuildIdMobalytics.Contains("builds", StringComparison.OrdinalIgnoreCase))
                {
                    urlValidStatusMobalytics = TranslationSource.Instance["rsCapBuildUrlDetected"];
                }
                else if(BuildIdMobalytics.Contains("mobalytics.gg", StringComparison.OrdinalIgnoreCase) &&
                   BuildIdMobalytics.Contains("profile", StringComparison.OrdinalIgnoreCase))
                {
                    urlValidStatusMobalytics = TranslationSource.Instance["rsCapProfileUrlDetected"];
                }
                else
                {
                    urlValidStatusMobalytics = TranslationSource.Instance["rsCapBuildProfileUrlMissing"];
                }

                return urlValidStatusMobalytics;            
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private bool CanAddD2CoreBuildExecute()
        {
            if (!BuildIdorUrlD2Core.Contains("d2core.com", StringComparison.OrdinalIgnoreCase) ||
                !BuildIdorUrlD2Core.Contains("bd=", StringComparison.OrdinalIgnoreCase)) return false;

            var urlparts = BuildIdorUrlD2Core.Split("bd=", StringSplitOptions.RemoveEmptyEntries);
            var buildId = urlparts.Count() == 2 ? urlparts[1] : string.Empty;

            // Remove extra parameters, e.g. lang
            buildId = buildId.Contains("&") ? buildId.Substring(0, buildId.IndexOf("&")) : buildId;

            bool isValid = !string.IsNullOrWhiteSpace(buildId) && buildId.Length == 4;
            BuildIdD2Core = isValid ? buildId : string.Empty;

            return isValid;
        }

        private async void AddD2CoreBuildExecute()
        {
            _ = Task.Factory.StartNew(() =>
            {
                _buildsManagerD2Core.DownloadD2CoreBuild(BuildIdD2Core);
            });

            var d2CoreDownloadDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapDownloadingWait"] };
            var dataContext = new D2CoreDownloadViewModel(async instance =>
            {
                await d2CoreDownloadDialog.WaitUntilUnloadedAsync();
            });
            d2CoreDownloadDialog.Content = new D2CoreDownloadView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, d2CoreDownloadDialog);
            await d2CoreDownloadDialog.WaitUntilUnloadedAsync();

            // Dispose VM to unregister message handlers
            (dataContext as IDisposable)?.Dispose();
        }

        private bool CanAddD4BuildsBuildExecute()
        {
            if (BuildIdorUrlD4Builds.Contains("mobalytics.gg", StringComparison.OrdinalIgnoreCase)) return false;

            var urlparts = BuildIdorUrlD4Builds.Split("/", StringSplitOptions.RemoveEmptyEntries).ToList();
            var buildIdContainer = urlparts.MaxBy(u => u.Length) ?? string.Empty;
            var urlpartsFinal = buildIdContainer.Split("?", StringSplitOptions.RemoveEmptyEntries).ToList();
            var buildId = urlpartsFinal.MaxBy(u => u.Length) ?? string.Empty;

            bool isValid = !string.IsNullOrWhiteSpace(buildId) && buildId.Length == 36;
            BuildIdD4Builds = isValid ? buildId : string.Empty;

            return isValid;
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

            // Dispose VM to unregister message handlers
            (dataContext as IDisposable)?.Dispose();
        }

        private bool CanAddMaxrollBuildExecute()
        {
            var urlparts = BuildIdorUrlMaxroll.Split("/", StringSplitOptions.RemoveEmptyEntries).ToList();
            var buildId = urlparts.Count > 0 ? urlparts[urlparts.Count - 1] : string.Empty;
            buildId = buildId.Contains("#") ? buildId.Substring(0, buildId.IndexOf("#")) : buildId;

            bool isValid = !string.IsNullOrWhiteSpace(buildId) && buildId.Length == 8 && !buildId.Contains("#");
            BuildIdMaxroll = isValid ? buildId : string.Empty;

            return isValid;
        }

        private void AddMaxrollBuildExecute()
        {
            _buildsManagerMaxroll.DownloadMaxrollBuild(BuildIdMaxroll);
        }

        private bool CanAddMobalyticsBuildExecute()
        {
            return !string.IsNullOrWhiteSpace(BuildIdMobalytics) && BuildIdMobalytics.StartsWith("https://mobalytics.gg") &&
                ((BuildIdMobalytics.Contains("profile") && !BuildIdMobalytics.Contains("builds")) ||
                (BuildIdMobalytics.Contains("profile") && BuildIdMobalytics.Contains("builds")));
        }

        private async void AddMobalyticsBuildExecute()
        {
            _ = Task.Factory.StartNew(() =>
            {
                _buildsManagerMobalytics.DownloadMobalyticsBuild(BuildIdMobalytics);
            });

            var mobalyticsDownloadDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapDownloadingWait"] };
            var dataContext = new MobalyticsDownloadViewModel(async instance =>
            {
                await mobalyticsDownloadDialog.WaitUntilUnloadedAsync();
            });
            mobalyticsDownloadDialog.Content = new MobalyticsDownloadView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, mobalyticsDownloadDialog);
            await mobalyticsDownloadDialog.WaitUntilUnloadedAsync();

            // Select build or profile tab
            if (_buildIdMobalytics.Contains("profile") && _buildIdMobalytics.Contains("builds"))
            {
                SelectedTabIndexMobalytics = 0;
            }
            else if (_buildIdMobalytics.Contains("profile"))
            {
                SelectedTabIndexMobalytics = 1;
            }

            // Dispose VM to unregister message handlers
            (dataContext as IDisposable)?.Dispose();
        }

        private async void AddD2CoreBuildAsPresetExecute(D2CoreBuildDataVariantJson? d2CoreBuildDataVariantJson)
        {
            if (d2CoreBuildDataVariantJson == null) return;

            // Show dialog to modify preset name
            StringWrapper presetName = new StringWrapper
            {
                String = SelectedD2CoreBuild.Name
            };

            List<string> presetNameSuggestions = new List<string>();
            presetNameSuggestions.Add($"{SelectedD2CoreBuild.Name}");
            presetNameSuggestions.Add($"{d2CoreBuildDataVariantJson.Name}");

            var setPresetNameDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapConfirmName"] };
            var dataContext = new SetPresetNameViewModel(async instance =>
            {
                await setPresetNameDialog.WaitUntilUnloadedAsync();
            }, presetName, presetNameSuggestions);
            setPresetNameDialog.Content = new SetPresetNameView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setPresetNameDialog);
            await setPresetNameDialog.WaitUntilUnloadedAsync();

            // Add confirmed preset name.
            if (!dataContext.IsCanceled)
            {
                SelectedD4BuildsBuild = new();
                SelectedMaxrollBuild = new();
                SelectedMobalyticsBuild = new();
                _buildsManagerD2Core.CreatePresetFromD2CoreBuild(SelectedD2CoreBuild, d2CoreBuildDataVariantJson.Name, presetName.String);
            }
        }

        private async void AddD4BuildsBuildAsPresetExecute(D4BuildsBuildVariant? d4BuildsBuildVariant)
        {
            if (d4BuildsBuildVariant == null) return;

            // Show dialog to modify preset name
            StringWrapper presetName = new StringWrapper
            {
                String = SelectedD4BuildsBuild.Name
            };

            List<string> presetNameSuggestions = new List<string>();
            presetNameSuggestions.Add($"{SelectedD4BuildsBuild.Name}");
            presetNameSuggestions.Add($"{d4BuildsBuildVariant.Name}");
            presetNameSuggestions.Add($"{SelectedD4BuildsBuild.Name} - {d4BuildsBuildVariant.Name}");
            presetNameSuggestions.Add($"{d4BuildsBuildVariant.Name} - {SelectedD4BuildsBuild.Name}");

            var setPresetNameDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapConfirmName"] };
            var dataContext = new SetPresetNameViewModel(async instance =>
            {
                await setPresetNameDialog.WaitUntilUnloadedAsync();
            }, presetName, presetNameSuggestions);
            setPresetNameDialog.Content = new SetPresetNameView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setPresetNameDialog);
            await setPresetNameDialog.WaitUntilUnloadedAsync();

            // Add confirmed preset name.
            if (!dataContext.IsCanceled)
            {
                SelectedD2CoreBuild = new();
                SelectedMaxrollBuild = new();
                SelectedMobalyticsBuild = new();
                _buildsManagerD4Builds.CreatePresetFromD4BuildsBuild(d4BuildsBuildVariant, SelectedD4BuildsBuild.Name, presetName.String);
            }
        }

        private async void AddMaxrollBuildAsPresetExecute(MaxrollBuildDataProfileJson? maxrollBuildDataProfileJson)
        {
            if (maxrollBuildDataProfileJson == null) return;

            // Show dialog to modify preset name
            StringWrapper presetName = new StringWrapper
            {
                String = SelectedMaxrollBuild.Name
            };

            List<string> presetNameSuggestions = new List<string>();
            presetNameSuggestions.Add($"{SelectedMaxrollBuild.Name}");
            presetNameSuggestions.Add($"{maxrollBuildDataProfileJson.Name}");
            presetNameSuggestions.Add($"{SelectedMaxrollBuild.Name} - {maxrollBuildDataProfileJson.Name}");
            presetNameSuggestions.Add($"{maxrollBuildDataProfileJson.Name} - {SelectedMaxrollBuild.Name}");

            var setPresetNameDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapConfirmName"] };
            var dataContext = new SetPresetNameViewModel(async instance =>
            {
                await setPresetNameDialog.WaitUntilUnloadedAsync();
            }, presetName, presetNameSuggestions);
            setPresetNameDialog.Content = new SetPresetNameView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setPresetNameDialog);
            await setPresetNameDialog.WaitUntilUnloadedAsync();

            // Add confirmed preset name.
            if (!dataContext.IsCanceled)
            {
                SelectedD2CoreBuild = new();
                SelectedD4BuildsBuild = new();
                SelectedMobalyticsBuild = new();
                _buildsManagerMaxroll.CreatePresetFromMaxrollBuild(SelectedMaxrollBuild, maxrollBuildDataProfileJson.Name, presetName.String);
            }
        }

        private async void AddMobalyticsBuildAsPresetExecute(MobalyticsBuildVariant? mobalyticsBuildVariant)
        {
            if (mobalyticsBuildVariant == null) return;

            // Show dialog to modify preset name
            StringWrapper presetName = new StringWrapper
            {
                String = SelectedMobalyticsBuild.Name
            };

            List<string> presetNameSuggestions = new List<string>();
            presetNameSuggestions.Add($"{SelectedMobalyticsBuild.Name}");
            presetNameSuggestions.Add($"{mobalyticsBuildVariant.Name}");
            presetNameSuggestions.Add($"{SelectedMobalyticsBuild.Name} - {mobalyticsBuildVariant.Name}");
            presetNameSuggestions.Add($"{mobalyticsBuildVariant.Name} - {SelectedMobalyticsBuild.Name}");

            var setPresetNameDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapConfirmName"] };
            var dataContext = new SetPresetNameViewModel(async instance =>
            {
                await setPresetNameDialog.WaitUntilUnloadedAsync();
            }, presetName, presetNameSuggestions);
            setPresetNameDialog.Content = new SetPresetNameView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setPresetNameDialog);
            await setPresetNameDialog.WaitUntilUnloadedAsync();

            // Add confirmed preset name.
            if (!dataContext.IsCanceled)
            {
                SelectedD2CoreBuild = new();
                SelectedD4BuildsBuild = new();
                SelectedMaxrollBuild = new();
                _buildsManagerMobalytics.CreatePresetFromMobalyticsBuild(mobalyticsBuildVariant, SelectedMobalyticsBuild.Name, presetName.String);
            }
        }

        private void AddMobalyticsProfileBuildVariantExecute(MobalyticsProfileBuildVariant? mobalyticsProfileBuildVariant)
        {
            if (mobalyticsProfileBuildVariant == null) return;

            BuildIdMobalytics = mobalyticsProfileBuildVariant.Url;
        }

        private bool CanExportAffixPresetCommandExecute()
        {
            return SelectedAffixPreset != null && !string.IsNullOrWhiteSpace(SelectedAffixPreset.Name);
        }

        private void ExportAffixPresetCommandExecute()
        {
            string fileName = $"Exports/{SelectedAffixPreset.Name}.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, SelectedAffixPreset, options);

            Process.Start("explorer.exe", path);
        }

        private void ImportAffixPresetDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private bool CanMergeBuildsExecute()
        {
            return !string.IsNullOrWhiteSpace(SelectedAffixPresetBuild1.Name)
                && !string.IsNullOrWhiteSpace(SelectedAffixPresetBuild2.Name);
        }

        private async void MergeBuildsExecute()
        {
            // Confirm name
            StringWrapper presetName = new StringWrapper
            {
                String = SelectedAffixPresetBuild1.Name
            };

            List<string> presetNameSuggestions = new List<string>();
            presetNameSuggestions.Add($"{SelectedAffixPresetBuild1.Name}");
            presetNameSuggestions.Add($"{SelectedAffixPresetBuild2.Name}");

            var setPresetNameDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapConfirmName"] };
            var dataContext = new SetPresetNameViewModel(async instance =>
            {
                await setPresetNameDialog.WaitUntilUnloadedAsync();
            }, presetName, presetNameSuggestions);
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
                    Color = ChangeColorBuild1 ? ColorBuild1 : a.Color,
                    IsGreater = a.IsGreater,
                    IsImplicit = a.IsImplicit,
                    IsTempered = a.IsTempered
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
                        IsGreater = itemAffixBuild2.IsGreater,
                        IsImplicit = itemAffixBuild2.IsImplicit,
                        IsTempered = itemAffixBuild2.IsTempered
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
                        Color = ChangeColorBuild2 ? ColorBuild2 : itemAspectBuild2.Color
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
                        Color = ChangeColorBuild2 ? ColorBuild2 : itemSigilsBuild2.Color
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

        public void Receive(AffixPresetAddedMessage message)
        {
            UpdateAffixPresets();

            string presetName = !string.IsNullOrWhiteSpace(SelectedD2CoreBuild?.Name) ? SelectedD2CoreBuild.Name :
                !string.IsNullOrWhiteSpace(SelectedD4BuildsBuild?.Name) ? SelectedD4BuildsBuild.Name :
                !string.IsNullOrWhiteSpace(SelectedMaxrollBuild?.Name) ? SelectedMaxrollBuild.Name :
                !string.IsNullOrWhiteSpace(SelectedMobalyticsBuild?.Name) ? SelectedMobalyticsBuild.Name : string.Empty;
            if (string.IsNullOrWhiteSpace(presetName)) return;

            // Select added preset
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(presetName));
            if (preset != null)
            {
                SelectedAffixPreset = preset;
            }
        }

        public void Receive(AffixPresetRemovedMessage message)
        {
            UpdateAffixPresets();

            // Select first preset
            if (AffixPresets.Count > 0)
            {
                SelectedAffixPreset = AffixPresets[0];
            }
        }

        public void Receive(D2CoreBuildsLoadedMessage message)
        {
            UpdateD2CoreBuilds();
        }

        public void Receive(D4BuildsBuildsLoadedMessage message)
        {
            UpdateD4BuildsBuilds();
        }

        public void Receive(MaxrollBuildsLoadedMessage message)
        {
            UpdateMaxrollBuilds();
        }

        public void Receive(MobalyticsBuildsLoadedMessage message)
        {
            UpdateMobalyticsBuilds();
        }

        public void Receive(MobalyticsProfilesLoadedMessage message)
        {
            UpdateMobalyticsProfiles();
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

        private void RemoveD2CoreBuildExecute(D2CoreBuild? d2CoreBuild)
        {
            if (d2CoreBuild != null)
            {
                _buildsManagerD2Core.RemoveD2CoreBuild(d2CoreBuild.Id);
            }
        }

        private void RemoveD4BuildsBuildExecute(D4BuildsBuild? d4BuildsBuild)
        {
            if (d4BuildsBuild != null)
            {
                _buildsManagerD4Builds.RemoveD4BuildsBuild(d4BuildsBuild.Id);
            }
        }

        private void RemoveMaxrollBuildExecute(MaxrollBuild? maxrollBuild)
        {
            if (maxrollBuild != null)
            {
                _buildsManagerMaxroll.RemoveMaxrollBuild(maxrollBuild.Id);
            }
        }

        private void RemoveMobalyticsBuildExecute(MobalyticsBuild? mobalyticsBuild)
        {
            if (mobalyticsBuild != null)
            {
                _buildsManagerMobalytics.RemoveMobalyticsBuild(mobalyticsBuild.Id);
            }
        }

        private void RemoveMobalyticsProfileExecute(MobalyticsProfile? mobalyticsProfile)
        {
            if (mobalyticsProfile != null)
            {
                _buildsManagerMobalytics.RemoveMobalyticsProfile(mobalyticsProfile.Id);
            }
        }

        private void SelectD2CoreBuildExecute(D2CoreBuild? d2CoreBuild)
        {
            if (d2CoreBuild != null)
            {
                SelectedD2CoreBuild = d2CoreBuild;
            }
        }

        private void SelectD4BuildsBuildExecute(D4BuildsBuild? d4BuildsBuild)
        {
            if (d4BuildsBuild != null)
            {
                SelectedD4BuildsBuild = d4BuildsBuild;
            }
        }

        private void SelectMaxrollBuildExecute(MaxrollBuild? maxrollBuild)
        {
            if (maxrollBuild != null)
            {
                SelectedMaxrollBuild = maxrollBuild;
            }
        }

        private void SelectMobalyticsBuildExecute(MobalyticsBuild? mobalyticsBuild)
        {
            if (mobalyticsBuild != null)
            {
                SelectedMobalyticsBuild = mobalyticsBuild;
            }
        }

        private void SelectMobalyticsProfileExecute(MobalyticsProfile? mobalyticsProfile)
        {
            if (mobalyticsProfile != null)
            {
                SelectedMobalyticsProfile = mobalyticsProfile;
            }
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

        private async void UpdateD2CoreBuildExecute(D2CoreBuild? build)
        {
            if (build == null) return;

            _ = Task.Factory.StartNew(() =>
            {
                _buildsManagerD2Core.DownloadD2CoreBuild(build.Id);
            });

            var d2CoreDownloadDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapDownloadingWait"] };
            var dataContext = new D2CoreDownloadViewModel(async instance =>
            {
                await d2CoreDownloadDialog.WaitUntilUnloadedAsync();
            });
            d2CoreDownloadDialog.Content = new D2CoreDownloadView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, d2CoreDownloadDialog);
            await d2CoreDownloadDialog.WaitUntilUnloadedAsync();

            // Dispose VM to unregister message handlers
            (dataContext as IDisposable)?.Dispose();
        }

        private async void UpdateD4BuildsBuildExecute(D4BuildsBuild? build)
        {
            if (build == null) return;

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

            // Dispose VM to unregister message handlers
            (dataContext as IDisposable)?.Dispose();
        }

        private void UpdateMaxrollBuildExecute(MaxrollBuild? build)
        {
            if (build == null) return;

            _buildsManagerMaxroll.DownloadMaxrollBuild(build.Id);
        }

        private async void UpdateMobalyticsBuildExecute(MobalyticsBuild? build)
        {
            if (build == null) return;

            _ = Task.Factory.StartNew(() =>
            {
                _buildsManagerMobalytics.DownloadMobalyticsBuild(build.Url);
            });

            var mobalyticsDownloadDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapDownloadingWait"] };
            var dataContext = new MobalyticsDownloadViewModel(async instance =>
            {
                await mobalyticsDownloadDialog.WaitUntilUnloadedAsync();
            });
            mobalyticsDownloadDialog.Content = new MobalyticsDownloadView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, mobalyticsDownloadDialog);
            await mobalyticsDownloadDialog.WaitUntilUnloadedAsync();

            // Select build or profile tab
            if (_buildIdMobalytics.Contains("profile") && _buildIdMobalytics.Contains("builds"))
            {
                SelectedTabIndexMobalytics = 0;
            }
            else if (_buildIdMobalytics.Contains("profile"))
            {
                SelectedTabIndexMobalytics = 1;
            }

            // Dispose VM to unregister message handlers
            (dataContext as IDisposable)?.Dispose();
        }

        private async void UpdateMobalyticsProfileExecute(MobalyticsProfile? profile)
        {
            if (profile == null) return;

            _ = Task.Factory.StartNew(() =>
            {
                _buildsManagerMobalytics.DownloadMobalyticsBuild(profile.Url);
            });

            var mobalyticsDownloadDialog = new CustomDialog() { Title = TranslationSource.Instance["rsCapDownloadingWait"] };
            var dataContext = new MobalyticsDownloadViewModel(async instance =>
            {
                await mobalyticsDownloadDialog.WaitUntilUnloadedAsync();
            });
            mobalyticsDownloadDialog.Content = new MobalyticsDownloadView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, mobalyticsDownloadDialog);
            await mobalyticsDownloadDialog.WaitUntilUnloadedAsync();

            // Select build or profile tab
            if (_buildIdMobalytics.Contains("profile") && _buildIdMobalytics.Contains("builds"))
            {
                SelectedTabIndexMobalytics = 0;
            }
            else if (_buildIdMobalytics.Contains("profile"))
            {
                SelectedTabIndexMobalytics = 1;
            }

            // Dispose VM to unregister message handlers
            (dataContext as IDisposable)?.Dispose();
        }

        private void VisitD2CoreExecute()
        {
            string uri = @"https://www.d2core.com/d4/builds";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
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

        private void VisitMobalyticsExecute()
        {
            string uri = @"https://mobalytics.gg/diablo-4/builds";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        private void WebD2CoreBuildExecute(D2CoreBuild? d2CoreBuild)
        {
            if (d2CoreBuild == null) return;

            string uri = @$"https://www.d2core.com/d4/planner?bd={d2CoreBuild.Id}";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        private void WebD4BuildsBuildExecute(D4BuildsBuild? d4BuildsBuild)
        {
            if (d4BuildsBuild == null) return;

            string uri = @$"https://d4builds.gg/builds/{d4BuildsBuild.Id}";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        private void WebMaxrollBuildExecute(MaxrollBuild? maxrollBuild)
        {
            if (maxrollBuild == null) return;

            string uri = @$"https://maxroll.gg/d4/planner/{maxrollBuild.Id}";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        private void WebMobalyticsBuildExecute(MobalyticsBuild? mobalyticsBuild)
        {
            if (mobalyticsBuild == null) return;

            Process.Start(new ProcessStartInfo(mobalyticsBuild.Url) { UseShellExecute = true });
        }

        private void WebMobalyticsProfileExecute(MobalyticsProfile? mobalyticsProfile)
        {
            if (mobalyticsProfile == null) return;

            Process.Start(new ProcessStartInfo(mobalyticsProfile.Url) { UseShellExecute = true });
        }

        #endregion

        // Start of Methods region

        #region Methods

        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

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

        private void UpdateD2CoreBuilds()
        {
            Application.Current?.Dispatcher.Invoke((Delegate)(() =>
            {
                D2CoreBuilds.Clear();
                D2CoreBuilds.AddRange(_buildsManagerD2Core.D2CoreBuilds);
                SelectedD2CoreBuild = new();
            }));
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
                MaxrollBuilds.AddRange(_buildsManagerMaxroll.MaxrollBuilds);
                SelectedMaxrollBuild = new();
            });
        }

        private void UpdateMobalyticsBuilds()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                MobalyticsBuilds.Clear();
                MobalyticsBuilds.AddRange(_buildsManagerMobalytics.MobalyticsBuilds);
                SelectedMobalyticsBuild = new();
            });
        }

        private void UpdateMobalyticsProfiles()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                MobalyticsProfiles.Clear();
                MobalyticsProfiles.AddRange(_buildsManagerMobalytics.MobalyticsProfiles);
                SelectedMobalyticsProfile = new();
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
                WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                {
                    Message = $"Failed to import {fileName}"
                }));
            }
        }

        #endregion
    }
}
