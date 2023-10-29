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
        private ObservableCollection<MaxrollBuildDescription> _affixPresetsMaxroll = new();

        private AffixPreset _selectedAffixPreset = new AffixPreset();
        private MaxrollBuildDescription _selectedAffixPresetMaxroll = new();

        // Start of Constructors region

        #region Constructors

        public ImportAffixPresetViewModel(Action<ImportAffixPresetViewModel> closeHandler, IAffixManager affixManager, IBuildsManager buildsManager)
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
            _eventAggregator.GetEvent<AffixPresetAddedEvent>().Subscribe(HandleAffixPresetAddedEvent);
            _eventAggregator.GetEvent<AffixPresetRemovedEvent>().Subscribe(HandleAffixPresetRemovedEvent);

            // Init services
            _logger = (ILogger<ImportAffixPresetViewModel>)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ILogger<ImportAffixPresetViewModel>));
            _affixManager = affixManager;
            _buildsManager = buildsManager;
            _dialogCoordinator = (IDialogCoordinator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IDialogCoordinator));

            // Init View commands
            CloseCommand = new DelegateCommand<ImportAffixPresetViewModel>(closeHandler);
            ImportAffixPresetDoneCommand = new DelegateCommand(ImportAffixPresetDoneExecute);
            ImportAffixPresetMaxrollCommand = new DelegateCommand(ImportAffixPresetMaxrollExecute);
            RemoveAffixPresetNameCommand = new DelegateCommand(RemoveAffixPresetNameExecute, CanRemoveAffixPresetNameExecute);
            VisitMaxrollCommand = new DelegateCommand<object>(VisitMaxrollExecute);

            // Load affix presets
            UpdateAffixPresets();

            // Load Maxroll presets
            UpdateAffixPresetsMaxroll();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<AffixPreset> AffixPresets { get => _affixPresets; set => _affixPresets = value; }
        public ObservableCollection<MaxrollBuildDescription> AffixPresetsMaxroll { get => _affixPresetsMaxroll; set => _affixPresetsMaxroll = value; }

        public DelegateCommand<ImportAffixPresetViewModel> CloseCommand { get; }
        public DelegateCommand ImportAffixPresetDoneCommand { get; }
        public DelegateCommand ImportAffixPresetMaxrollCommand { get; }
        public DelegateCommand RemoveAffixPresetNameCommand { get; }
        public DelegateCommand<object> VisitMaxrollCommand { get; }

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

        public MaxrollBuildDescription SelectedAffixPresetMaxroll
        {
            get => _selectedAffixPresetMaxroll;
            set
            {
                _selectedAffixPresetMaxroll = value;
                if (value == null)
                {
                    _selectedAffixPresetMaxroll = new();
                }
                RaisePropertyChanged(nameof(SelectedAffixPresetMaxroll));
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleAffixPresetAddedEvent()
        {
            UpdateAffixPresets();

            // Select added preset
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(SelectedAffixPresetMaxroll.Name));
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

        private void ImportAffixPresetDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private void ImportAffixPresetMaxrollExecute()
        {
            if(SelectedAffixPresetMaxroll != null && !string.IsNullOrWhiteSpace(SelectedAffixPresetMaxroll.Name))
            {
                _buildsManager.DownloadMaxrollBuild(SelectedAffixPresetMaxroll.Name);
            }
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

        private void VisitMaxrollExecute(object uri)
        {
            Process.Start(new ProcessStartInfo(uri as string ?? string.Empty) { UseShellExecute = true });
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

        private void UpdateAffixPresetsMaxroll()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                AffixPresetsMaxroll.Clear();

                foreach (var buildWrapper in _buildsManager.MaxrollBuilds)
                {
                    foreach (var build in buildWrapper)
                    {
                        AffixPresetsMaxroll.Add(new MaxrollBuildDescription
                        {
                            Name = build.Key,
                            Uri = build.Value
                        });
                    }
                }

                if (AffixPresetsMaxroll.Any())
                {
                    SelectedAffixPresetMaxroll = AffixPresetsMaxroll[0];
                }
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
