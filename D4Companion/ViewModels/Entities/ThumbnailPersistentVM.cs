using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace D4Companion.ViewModels.Entities
{
    public class ThumbnailPersistentVM : ObservableObject
    {
        private ThumbnailPersistent _thumbnail = new();

        // Start of Constructors region

        #region Constructors

        public ThumbnailPersistentVM(ThumbnailPersistent thumbnail)
        {
            _thumbnail = thumbnail;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public bool IsEnabled
        {
            get => _thumbnail.IsEnabled;
            set
            {
                _thumbnail.IsEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public ThumbnailPersistent Model
        {
            get => _thumbnail;
        }

        public string Name
        {
            get => _thumbnail.Name;
        }

        public int DestinationPositionX
        {
            get => _thumbnail.DestinationPositionX;
            set
            {
                _thumbnail.DestinationPositionX = value;
                OnPropertyChanged(nameof(DestinationPositionX));

                WeakReferenceMessenger.Default.Send(new ThumbnailROIUpdatedMessage());
            }
        }

        public int DestinationPositionY
        {
            get => _thumbnail.DestinationPositionY;
            set
            {
                _thumbnail.DestinationPositionY = value;
                OnPropertyChanged(nameof(DestinationPositionY));

                WeakReferenceMessenger.Default.Send(new ThumbnailROIUpdatedMessage());
            }
        }

        public int DestinationWidth
        {
            get => _thumbnail.DestinationWidth;
            set
            {
                _thumbnail.DestinationWidth = value;
                OnPropertyChanged(nameof(DestinationWidth));

                WeakReferenceMessenger.Default.Send(new ThumbnailROIUpdatedMessage());
            }
        }

        public int DestinationHeight
        {
            get => _thumbnail.DestinationHeight;
            set
            {
                _thumbnail.DestinationHeight = value;
                OnPropertyChanged(nameof(DestinationHeight));

                WeakReferenceMessenger.Default.Send(new ThumbnailROIUpdatedMessage());
            }
        }

        public int SourcePositionX
        {
            get => _thumbnail.SourcePositionX;
            set
            {
                _thumbnail.SourcePositionX = value;
                OnPropertyChanged(nameof(SourcePositionX));

                WeakReferenceMessenger.Default.Send(new ThumbnailROIUpdatedMessage());
            }
        }

        public int SourcePositionY
        {
            get => _thumbnail.SourcePositionY;
            set
            {
                _thumbnail.SourcePositionY = value;
                OnPropertyChanged(nameof(SourcePositionY));

                WeakReferenceMessenger.Default.Send(new ThumbnailROIUpdatedMessage());
            }
        }

        public int SourceWidth
        {
            get => _thumbnail.SourceWidth;
            set
            {
                _thumbnail.SourceWidth = value;
                OnPropertyChanged(nameof(SourceWidth));

                WeakReferenceMessenger.Default.Send(new ThumbnailROIUpdatedMessage());
            }
        }

        public int SourceHeight
        {
            get => _thumbnail.SourceHeight;
            set
            {
                _thumbnail.SourceHeight = value;
                OnPropertyChanged(nameof(SourceHeight));

                WeakReferenceMessenger.Default.Send(new ThumbnailROIUpdatedMessage());
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
