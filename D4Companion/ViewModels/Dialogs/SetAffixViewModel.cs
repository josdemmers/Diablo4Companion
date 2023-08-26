using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Interfaces;
using D4Companion.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;

namespace D4Companion.ViewModels.Dialogs
{
    public class SetAffixViewModel : BindableBase
    {
        private readonly IAffixManager _affixManager;

        private AffixInfo _affixInfo = new AffixInfo();
        private string _imageHead = "/Images/head_icon.png";
        private string _imageTorso = "/Images/torso_icon.png";
        private string _imageHands = "/Images/hands_icon.png";
        private string _imageLegs = "/Images/legs_icon.png";
        private string _imageFeet = "/Images/feet_icon.png";
        private string _imageNeck = "/Images/neck_icon.png";
        private string _imageRing = "/Images/ring_icon.png";
        private string _imageMainHand = "/Images/mainhand_icon.png";
        private string _imageRanged = "/Images/ranged_icon.png";
        private string _imageOffHand = "/Images/offhand_icon.png";
        private bool _toggleHead = false;
        private bool _toggleTorso = false;
        private bool _toggleHands = false;
        private bool _toggleLegs = false;
        private bool _toggleFeet = false;
        private bool _toggleNeck = false;
        private bool _toggleRing = false;
        private bool _toggleMainHand = false;
        private bool _toggleRanged = false;
        private bool _toggleOffHand = false;

        // Start of Constructors region

        #region Constructors

        public SetAffixViewModel(Action<SetAffixViewModel> closeHandler, AffixInfo affixInfo)
        {
            // Init services
            _affixManager = (IAffixManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAffixManager));

            // Init View commands
            CloseCommand = new DelegateCommand<SetAffixViewModel>(closeHandler);
            SetAffixDoneCommand = new DelegateCommand(SetAffixDoneExecute);

            _affixInfo = affixInfo;

            // Init affix selection
            InitAffixSelection();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public DelegateCommand<SetAffixViewModel> CloseCommand { get; }
        public DelegateCommand SetAffixDoneCommand { get; }

        public string ImageHead { get => _imageHead; set => SetProperty(ref _imageHead, value, () => { RaisePropertyChanged(nameof(ImageHead)); }); }
        public string ImageTorso { get => _imageTorso; set => SetProperty(ref _imageTorso, value, () => { RaisePropertyChanged(nameof(ImageTorso)); }); }
        public string ImageHands { get => _imageHands; set => SetProperty(ref _imageHands, value, () => { RaisePropertyChanged(nameof(ImageHands)); }); }
        public string ImageLegs { get => _imageLegs; set => SetProperty(ref _imageLegs, value, () => { RaisePropertyChanged(nameof(ImageLegs)); }); }
        public string ImageFeet { get => _imageFeet; set => SetProperty(ref _imageFeet, value, () => { RaisePropertyChanged(nameof(ImageFeet)); }); }
        public string ImageNeck { get => _imageNeck; set => SetProperty(ref _imageNeck, value, () => { RaisePropertyChanged(nameof(ImageNeck)); }); }
        public string ImageRing { get => _imageRing; set => SetProperty(ref _imageRing, value, () => { RaisePropertyChanged(nameof(ImageRing)); }); }
        public string ImageMainHand { get => _imageMainHand; set => SetProperty(ref _imageMainHand, value, () => { RaisePropertyChanged(nameof(ImageMainHand)); }); }
        public string ImageRanged { get => _imageRanged; set => SetProperty(ref _imageRanged, value, () => { RaisePropertyChanged(nameof(ImageRanged)); }); }
        public string ImageOffHand { get => _imageOffHand; set => SetProperty(ref _imageOffHand, value, () => { RaisePropertyChanged(nameof(ImageOffHand)); }); }

        public AffixInfo AffixInfo
        {
            get => _affixInfo;
            set
            {
                _affixInfo = value;
                RaisePropertyChanged();
            }
        }

        public bool ToggleHead
        {
            get => _toggleHead;
            set
            {
                _toggleHead = value;
                RaisePropertyChanged();
                if (value)
                {
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Helm);
                }
                else
                {
                    _affixManager.RemoveAffix(AffixInfo, ItemTypeConstants.Helm);
                }
            }
        }

        public bool ToggleTorso
        {
            get => _toggleTorso;
            set
            {
                _toggleTorso = value;
                RaisePropertyChanged();
                if (value)
                {
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Chest);
                }
                else
                {
                    _affixManager.RemoveAffix(AffixInfo, ItemTypeConstants.Chest);
                }
            }
        }

        public bool ToggleHands
        {
            get => _toggleHands;
            set
            {
                _toggleHands = value;
                RaisePropertyChanged();
                if (value)
                {
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Gloves);
                }
                else
                {
                    _affixManager.RemoveAffix(AffixInfo, ItemTypeConstants.Gloves);
                }
            }
        }

        public bool ToggleLegs
        {
            get => _toggleLegs;
            set
            {
                _toggleLegs = value;
                RaisePropertyChanged();
                if (value)
                {
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Pants);
                }
                else
                {
                    _affixManager.RemoveAffix(AffixInfo, ItemTypeConstants.Pants);
                }
            }
        }

        public bool ToggleFeet
        {
            get => _toggleFeet;
            set
            {
                _toggleFeet = value;
                RaisePropertyChanged();
                if (value)
                {
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Boots);
                }
                else
                {
                    _affixManager.RemoveAffix(AffixInfo, ItemTypeConstants.Boots);
                }
            }
        }

        public bool ToggleNeck
        {
            get => _toggleNeck;
            set
            {
                _toggleNeck = value;
                RaisePropertyChanged();
                if (value)
                {
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Amulet);
                }
                else
                {
                    _affixManager.RemoveAffix(AffixInfo, ItemTypeConstants.Amulet);
                }
            }
        }

        public bool ToggleRing
        {
            get => _toggleRing;
            set
            {
                _toggleRing = value;
                RaisePropertyChanged();
                if (value)
                {
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Ring);
                }
                else
                {
                    _affixManager.RemoveAffix(AffixInfo, ItemTypeConstants.Ring);
                }
            }
        }

        public bool ToggleMainHand
        {
            get => _toggleMainHand;
            set
            {
                _toggleMainHand = value;
                RaisePropertyChanged();
                if (value)
                {
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Weapon);
                }
                else
                {
                    _affixManager.RemoveAffix(AffixInfo, ItemTypeConstants.Weapon);
                }
            }
        }

        public bool ToggleRanged
        {
            get => _toggleRanged;
            set
            {
                _toggleRanged = value;
                RaisePropertyChanged();
                if (value)
                {
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Ranged);
                }
                else
                {
                    _affixManager.RemoveAffix(AffixInfo, ItemTypeConstants.Ranged);
                }
            }
        }

        public bool ToggleOffHand
        {
            get => _toggleOffHand;
            set
            {
                _toggleOffHand = value;
                RaisePropertyChanged();
                if (value)
                {
                    _affixManager.AddAffix(AffixInfo, ItemTypeConstants.Offhand);
                }
                else
                {
                    _affixManager.RemoveAffix(AffixInfo, ItemTypeConstants.Offhand);
                }
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void SetAffixDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitAffixSelection()
        {
            ToggleHead = _affixManager.IsAffixSelected(AffixInfo, ItemTypeConstants.Helm);
            ToggleTorso = _affixManager.IsAffixSelected(AffixInfo, ItemTypeConstants.Chest);
            ToggleHands = _affixManager.IsAffixSelected(AffixInfo, ItemTypeConstants.Gloves);
            ToggleLegs = _affixManager.IsAffixSelected(AffixInfo, ItemTypeConstants.Pants);
            ToggleFeet = _affixManager.IsAffixSelected(AffixInfo, ItemTypeConstants.Boots);
            ToggleNeck = _affixManager.IsAffixSelected(AffixInfo, ItemTypeConstants.Amulet);
            ToggleRing = _affixManager.IsAffixSelected(AffixInfo, ItemTypeConstants.Ring);
            ToggleMainHand = _affixManager.IsAffixSelected(AffixInfo, ItemTypeConstants.Weapon);
            ToggleRanged = _affixManager.IsAffixSelected(AffixInfo, ItemTypeConstants.Ranged);
            ToggleOffHand = _affixManager.IsAffixSelected(AffixInfo, ItemTypeConstants.Offhand);
        }

        #endregion
    }
}
