using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D4Companion.Interfaces;
using D4Companion.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace D4Companion.ViewModels.Dialogs
{
    public class ColorsConfigViewModel : ObservableObject
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public ColorsConfigViewModel(Action<ColorsConfigViewModel?> closeHandler)
        {
            // Init services
            _dialogCoordinator = App.Current.Services.GetRequiredService<IDialogCoordinator>();
            _settingsManager = App.Current.Services.GetRequiredService<ISettingsManager>();

            // Init view commands
            CloseCommand = new RelayCommand<ColorsConfigViewModel>(closeHandler);
            ColorsConfigDoneCommand = new RelayCommand(ColorsConfigDoneExecute);
            SetAffixColorCommand = new RelayCommand<string>(SetAffixColorExecute);
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ICommand CloseCommand { get; }
        public ICommand ColorsConfigDoneCommand { get; }
        public ICommand SetAffixColorCommand { get; }

        public Color DefaultColorGreater
        {
            get => _settingsManager.Settings.DefaultColorGreater;
            set
            {
                _settingsManager.Settings.DefaultColorGreater = value;
                OnPropertyChanged(nameof(DefaultColorGreater));

                _settingsManager.SaveSettings();
            }
        }

        public Color DefaultColorImplicit
        {
            get => _settingsManager.Settings.DefaultColorImplicit;
            set
            {
                _settingsManager.Settings.DefaultColorImplicit = value;
                OnPropertyChanged(nameof(DefaultColorImplicit));

                _settingsManager.SaveSettings();
            }
        }

        public Color DefaultColorNormal
        {
            get => _settingsManager.Settings.DefaultColorNormal;
            set
            {
                _settingsManager.Settings.DefaultColorNormal = value;
                OnPropertyChanged(nameof(DefaultColorNormal));

                _settingsManager.SaveSettings();
            }
        }

        public Color DefaultColorTempered
        {
            get => _settingsManager.Settings.DefaultColorTempered;
            set
            {
                _settingsManager.Settings.DefaultColorTempered = value;
                OnPropertyChanged(nameof(DefaultColorTempered));

                _settingsManager.SaveSettings();
            }
        }

        public Color DefaultColorAspects
        {
            get => _settingsManager.Settings.DefaultColorAspects;
            set
            {
                _settingsManager.Settings.DefaultColorAspects = value;
                OnPropertyChanged(nameof(DefaultColorAspects));

                _settingsManager.SaveSettings();
            }
        }

        public Color DefaultColorUniques
        {
            get => _settingsManager.Settings.DefaultColorUniques;
            set
            {
                _settingsManager.Settings.DefaultColorUniques = value;
                OnPropertyChanged(nameof(DefaultColorUniques));

                _settingsManager.SaveSettings();
            }
        }

        public Color DefaultColorRunes
        {
            get => _settingsManager.Settings.DefaultColorRunes;
            set
            {
                _settingsManager.Settings.DefaultColorRunes = value;
                OnPropertyChanged(nameof(DefaultColorRunes));

                _settingsManager.SaveSettings();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void ColorsConfigDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        private async void SetAffixColorExecute(string? affixType)
        {
            Color currentColor = string.IsNullOrWhiteSpace(affixType) ? DefaultColorNormal :
                affixType.Equals("Implicit") ? DefaultColorImplicit :
                affixType.Equals("Normal") ? DefaultColorNormal :
                affixType.Equals("Greater") ? DefaultColorGreater :
                affixType.Equals("Tempered") ? DefaultColorTempered :
                affixType.Equals("Aspects") ? DefaultColorAspects :
                affixType.Equals("Uniques") ? DefaultColorUniques :
                affixType.Equals("Runes") ? DefaultColorRunes : DefaultColorNormal;

            var setAffixColorDialog = new CustomDialog() { Title = "Set affix color" };
            var dataContext = new SetAffixTypeColorViewModel(async instance =>
            {
                await setAffixColorDialog.WaitUntilUnloadedAsync();
            }, currentColor);
            setAffixColorDialog.Content = new SetAffixColorView() { DataContext = dataContext };
            await _dialogCoordinator.ShowMetroDialogAsync(this, setAffixColorDialog);
            await setAffixColorDialog.WaitUntilUnloadedAsync();

            switch (affixType)
            {
                case "Implicit":
                    DefaultColorImplicit = dataContext.SelectedColor.Value;
                    break;
                case "Normal":
                    DefaultColorNormal = dataContext.SelectedColor.Value;
                    break;
                case "Greater":
                    DefaultColorGreater = dataContext.SelectedColor.Value;
                    break;
                case "Tempered":
                    DefaultColorTempered = dataContext.SelectedColor.Value;
                    break;
                case "Aspects":
                    DefaultColorAspects = dataContext.SelectedColor.Value;
                    break;
                case "Uniques":
                    DefaultColorUniques = dataContext.SelectedColor.Value;
                    break;
                case "Runes":
                    DefaultColorRunes = dataContext.SelectedColor.Value;
                    break;
                default:
                    break;
            }

            _settingsManager.SaveSettings();
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
