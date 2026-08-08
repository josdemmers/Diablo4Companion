using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.SystemPresets.Entities;
using D4Companion.SystemPresets.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace D4Companion.SystemPresets.ViewModels.Entities
{
    public class ScreenCaptureVM : ObservableObject
    {
        private ScreenCapture _screenCapture = new();
        
        private bool _isActive = false;

        // Start of Constructors region

        #region Constructors

        public ScreenCaptureVM(ScreenCapture screenCapture)
        {
            _screenCapture = screenCapture;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public BitmapSource? BitmapSource
        {
            get => _screenCapture.BitmapSource;
        }

        public string DeviceName
        {
            get => _screenCapture.DeviceName;
        }

        public bool IsActive 
        { 
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));

                WeakReferenceMessenger.Default.Send(new ActiveScreenChangedMessage(new ActiveScreenChangedMessageParams
                {
                    DeviceName = DeviceName,
                    IsActive = IsActive
                }));
            }
        }

        public DateTime? Timestamp
        {
            get => _screenCapture.Timestamp;
        }        

        public void Update()
        {
            OnPropertyChanged(nameof(BitmapSource));
            OnPropertyChanged(nameof(DeviceName));
            OnPropertyChanged(nameof(Timestamp));
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
