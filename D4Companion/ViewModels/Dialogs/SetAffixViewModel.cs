using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Extensions;
using D4Companion.Interfaces;
using D4Companion.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace D4Companion.ViewModels.Dialogs
{
    public class SetAffixViewModel : ObservableObject,
        IDisposable,
        IRecipient<SelectedAffixesChangedMessage>
    {
        private readonly IAffixManager _affixManager;

        private ObservableCollection<ItemAffixVM> _selectedAffixes = new ObservableCollection<ItemAffixVM>();

        private AffixInfo _affixInfo = new AffixInfo();
        private AffixPreset _affixPreset = new AffixPreset();
        private const string _imageHead = "/Images/head_icon.png";
        private const string _imageTorso = "/Images/torso_icon.png";
        private const string _imageHands = "/Images/hands_icon.png";
        private const string _imageLegs = "/Images/legs_icon.png";
        private const string _imageFeet = "/Images/feet_icon.png";
        private const string _imageNeck = "/Images/neck_icon.png";
        private const string _imageRing = "/Images/ring_icon.png";
        private const string _imageMainHand = "/Images/mainhand_icon.png";
        private const string _imageRanged = "/Images/ranged_icon.png";
        private const string _imageOffHand = "/Images/offhand_icon.png";

        // Start of Constructors region

        #region Constructors

        public SetAffixViewModel(Action<SetAffixViewModel?> closeHandler, AffixPreset affixPreset, AffixInfo affixInfo)
        {
            // Init services
            _affixManager = App.Current.Services.GetRequiredService<IAffixManager>();

            // Init messages
            WeakReferenceMessenger.Default.RegisterAll(this);

            // Init view commands
            AddAffixCommand = new RelayCommand<string>(AddAffixExecute);
            CloseCommand = new RelayCommand<SetAffixViewModel>(closeHandler);
            RemoveAffixCommand = new RelayCommand<ItemAffixVM>(RemoveAffixExecute);
            SetAffixDoneCommand = new RelayCommand(SetAffixDoneExecute, CanSetAffixDoneExecute);

            _affixPreset = affixPreset;
            _affixInfo = affixInfo;

            // Init affix selection
            UpdateSelectedAffixes();

            // Init filter view
            CreateSelectedAffixesFilteredView();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<ItemAffixVM> SelectedAffixes { get => _selectedAffixes; set => _selectedAffixes = value; }
        public ListCollectionView? SelectedAffixesFiltered { get; private set; }

        public ICommand AddAffixCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand RemoveAffixCommand { get; }
        public ICommand SetAffixDoneCommand { get; }

        public int? AffixCounterHead
        {
            get
            {
                int count = SelectedAffixes.Count(a => a.Id.Equals(_affixInfo.IdName) && a.Type.Equals(Constants.ItemTypeConstants.Helm));
                return count > 0 ? count : null;
            }
        }

        public int? AffixCounterTorso
        {
            get
            {
                int count = SelectedAffixes.Count(a => a.Id.Equals(_affixInfo.IdName) && a.Type.Equals(Constants.ItemTypeConstants.Chest));
                return count > 0 ? count : null;
            }
        }

        public int? AffixCounterHands
        {
            get
            {
                int count = SelectedAffixes.Count(a => a.Id.Equals(_affixInfo.IdName) && a.Type.Equals(Constants.ItemTypeConstants.Gloves));
                return count > 0 ? count : null;
            }
        }

        public int? AffixCounterLegs
        {
            get
            {
                int count = SelectedAffixes.Count(a => a.Id.Equals(_affixInfo.IdName) && a.Type.Equals(Constants.ItemTypeConstants.Pants));
                return count > 0 ? count : null;
            }
        }

        public int? AffixCounterFeet
        {
            get
            {
                int count = SelectedAffixes.Count(a => a.Id.Equals(_affixInfo.IdName) && a.Type.Equals(Constants.ItemTypeConstants.Boots));
                return count > 0 ? count : null;
            }
        }

        public int? AffixCounterNeck
        {
            get
            {
                int count = SelectedAffixes.Count(a => a.Id.Equals(_affixInfo.IdName) && a.Type.Equals(Constants.ItemTypeConstants.Amulet));
                return count > 0 ? count : null;
            }
        }

        public int? AffixCounterRing
        {
            get
            {
                int count = SelectedAffixes.Count(a => a.Id.Equals(_affixInfo.IdName) && a.Type.Equals(Constants.ItemTypeConstants.Ring));
                return count > 0 ? count : null;
            }
        }

        public int? AffixCounterMainHand
        {
            get
            {
                int count = SelectedAffixes.Count(a => a.Id.Equals(_affixInfo.IdName) && a.Type.Equals(Constants.ItemTypeConstants.Weapon));
                return count > 0 ? count : null;
            }
        }

        public int? AffixCounterRanged
        {
            get
            {
                int count = SelectedAffixes.Count(a => a.Id.Equals(_affixInfo.IdName) && a.Type.Equals(Constants.ItemTypeConstants.Ranged));
                return count > 0 ? count : null;
            }
        }

        public int? AffixCounterOffHand
        {
            get
            {
                int count = SelectedAffixes.Count(a => a.Id.Equals(_affixInfo.IdName) && a.Type.Equals(Constants.ItemTypeConstants.Offhand));
                return count > 0 ? count : null;
            }
        }

        public double MinimalAffixValue
        {
            get => _affixManager.GetAffixMinimalValue(_affixInfo.IdName);
            set => _affixManager.SetAffixMinimalValue(_affixInfo.IdName, value);
        }

        public string ImageHead => _imageHead;
        public string ImageTorso => _imageTorso;
        public string ImageHands => _imageHands;
        public string ImageLegs => _imageLegs;
        public string ImageFeet => _imageFeet;
        public string ImageNeck => _imageNeck;
        public string ImageRing => _imageRing;
        public string ImageMainHand => _imageMainHand;
        public string ImageRanged => _imageRanged;
        public string ImageOffHand => _imageOffHand;

        public AffixInfo AffixInfo
        {
            get => _affixInfo;
            set
            {
                _affixInfo = value;
                OnPropertyChanged();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void AddAffixExecute(string? itemType)
        {
            switch (itemType)
            {
                case _imageHead:
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Helm);
                    break;
                case _imageTorso:
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Chest);
                    break;
                case _imageHands:
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Gloves);
                    break;
                case _imageLegs:
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Pants);
                    break;
                case _imageFeet:
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Boots);
                    break;
                case _imageNeck:
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Amulet);
                    break;
                case _imageRing:
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Ring);
                    break;
                case _imageMainHand:
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Weapon);
                    break;
                case _imageRanged:
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Ranged);
                    break;
                case _imageOffHand:
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Offhand);
                    break;
                default:
                    break;
            }
        }

        public void Receive(SelectedAffixesChangedMessage message)
        {
            UpdateSelectedAffixes();
            ((RelayCommand)SetAffixDoneCommand).NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(AffixCounterHead));
            OnPropertyChanged(nameof(AffixCounterTorso));
            OnPropertyChanged(nameof(AffixCounterHands));
            OnPropertyChanged(nameof(AffixCounterLegs));
            OnPropertyChanged(nameof(AffixCounterFeet));
            OnPropertyChanged(nameof(AffixCounterNeck));
            OnPropertyChanged(nameof(AffixCounterRing));
            OnPropertyChanged(nameof(AffixCounterMainHand));
            OnPropertyChanged(nameof(AffixCounterRanged));
            OnPropertyChanged(nameof(AffixCounterOffHand));
        }

        private void RemoveAffixExecute(ItemAffixVM? itemAffixVM)
        {
            if (itemAffixVM != null) 
            {
                _affixManager.RemoveAffix(itemAffixVM.Model);
            }
        }

        private bool CanSetAffixDoneExecute()
        {
            var duplicates = _selectedAffixes.ToList().FindAll(a => a.Id.Equals(_affixInfo.IdName))
                .GroupBy(a => new { a.Type, a.IsImplicit, a.IsGreater, a.IsTempered })
                .Where(g => g.Count() > 1);
            return duplicates.Count() == 0;
        }

        private void SetAffixDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void CreateSelectedAffixesFilteredView()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                SelectedAffixesFiltered = new ListCollectionView(SelectedAffixes)
                {
                    Filter = FilterSelectedAffixes
                };
            });
        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        private bool FilterSelectedAffixes(object selectedAffixObj)
        {
            if (selectedAffixObj == null) return false;

            ItemAffixVM itemAffix = (ItemAffixVM)selectedAffixObj;

            return itemAffix.Id.Equals(_affixInfo.IdName);
        }

        private void UpdateSelectedAffixes()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SelectedAffixes.Clear();
                if (_affixPreset != null)
                {
                    var itemAffixes = _affixPreset.ItemAffixes.Select(itemAffix => new ItemAffixVM(itemAffix)).ToList();
                    itemAffixes.Sort((x, y) =>
                    {
                        int result = x.Type.CompareTo(y.Type);
                        if (result == 0)
                        {
                            result = x.IsImplicit && !y.IsImplicit ? -1 : y.IsImplicit && !x.IsImplicit ? 1 : 0;
                        }
                        if (result == 0)
                        {
                            result = x.IsGreater && !y.IsGreater ? -1 : y.IsGreater && !x.IsGreater ? 1 : 0;
                        }
                        if (result == 0)
                        {
                            result = x.IsTempered && !y.IsTempered ? -1 : y.IsTempered && !x.IsTempered ? 1 : 0;
                        }

                        return result;
                    });

                    SelectedAffixes.AddRange(itemAffixes);
                }
            });
        }

        #endregion
    }
}
