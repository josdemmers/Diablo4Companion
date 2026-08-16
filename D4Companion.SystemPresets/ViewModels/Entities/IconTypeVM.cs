using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.SystemPresets.Entities;
using D4Companion.SystemPresets.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace D4Companion.SystemPresets.ViewModels.Entities
{
    public class IconTypeVM : ObservableObject
    {
        private IconType _iconType = new();

        // Start of Constructors region

        #region Constructors

        public IconTypeVM(IconType iconType)
        {
            _iconType = iconType;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public int Count 
        { 
            get => _iconType.Count;
        }

        public string DisplayName 
        { 
            get => _iconType.DisplayName;
        }

        public IconType Model
        {
            get => _iconType;
        }

        public string Name
        {
            get => _iconType.Name;
        }

        public string SelectedScreenshot
        {
            get => _iconType.SelectedScreenshot;
            set
            {
                _iconType.SelectedScreenshot = value;
                OnPropertyChanged(nameof(SelectedScreenshot));
            }
        }

        public int PositionX 
        {
            get => _iconType.PositionX;
            set
            {
                _iconType.PositionX = value;
                OnPropertyChanged(nameof(PositionX));

                WeakReferenceMessenger.Default.Send(new IconTypeROIUpdatedMessage());
            }
        }

        public int PositionY
        {
            get => _iconType.PositionY;
            set
            {
                _iconType.PositionY = value;
                OnPropertyChanged(nameof(PositionY));

                WeakReferenceMessenger.Default.Send(new IconTypeROIUpdatedMessage());
            }
        }

        public int Width
        {
            get => _iconType.Width;
            set
            {
                _iconType.Width = value;
                OnPropertyChanged(nameof(Width));

                WeakReferenceMessenger.Default.Send(new IconTypeROIUpdatedMessage());
            }
        }

        public int Height
        {
            get => _iconType.Height;
            set
            {
                _iconType.Height = value;
                OnPropertyChanged(nameof(Height));

                WeakReferenceMessenger.Default.Send(new IconTypeROIUpdatedMessage());
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}


