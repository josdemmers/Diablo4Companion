using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.ViewModels.Entities;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace D4Companion.ViewModels
{
    public class DebugViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private int? _badgeCount = null;
        private OcrResult _ocrResultAspect = new();

        private BitmapSource? _processedScreenItemTooltip = null;
        private BitmapSource? _processedScreenItemType = null;
        private BitmapSource? _processedScreenItemAffixLocations = null;
        private BitmapSource? _processedScreenItemAffixAreas = null;
        private BitmapSource? _processedScreenItemAffixes = null;
        private BitmapSource? _processedScreenItemAspectLocation = null;
        private BitmapSource? _processedScreenItemAspectArea = null;
        private BitmapSource? _processedScreenItemAspect = null;
        private BitmapSource? _processedScreenItemSocketLocations = null;

        private ObservableCollection<OcrResultDescriptor> _ocrResultAffixes = new();

        // Start of Constructors region

        #region Constructors

        public DebugViewModel(IEventAggregator eventAggregator, ILogger<DebugViewModel> logger, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ScreenProcessItemTooltipReadyEvent>().Subscribe(HandleScreenProcessItemTooltipReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemTypeReadyEvent>().Subscribe(HandleScreenProcessItemTypeReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAffixLocationsReadyEvent>().Subscribe(HandleScreenProcessItemAffixLocationsReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAffixAreasReadyEvent>().Subscribe(HandleScreenProcessItemAffixAreasReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAffixesReadyEvent>().Subscribe(HandleScreenProcessItemAffixesReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAffixesOcrReadyEvent>().Subscribe(HandleScreenProcessItemAffixesOcrReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAspectLocationReadyEvent>().Subscribe(HandleScreenProcessItemAspectLocationReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAspectAreaReadyEvent>().Subscribe(HandleScreenProcessItemAspectAreaReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAspectReadyEvent>().Subscribe(HandleScreenProcessItemAspectReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAspectOcrReadyEvent>().Subscribe(HandleScreenProcessItemAspectOcrReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemSocketLocationsReadyEvent>().Subscribe(HandleScreenProcessItemSocketLocationsReadyEvent);

            // Init logger
            _logger = logger;

            // Init services
            _settingsManager = settingsManager;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<OcrResultDescriptor> OcrResultAffixes { get => _ocrResultAffixes; set => _ocrResultAffixes = value; }

        public int AffixAreaHeightOffsetTop
        {
            get => _settingsManager.Settings.AffixAreaHeightOffsetTop;
            set
            {
                _settingsManager.Settings.AffixAreaHeightOffsetTop = value;
                RaisePropertyChanged(nameof(AffixAreaHeightOffsetTop));

                _settingsManager.SaveSettings();
            }
        }

        public int AffixAreaHeightOffsetBottom
        {
            get => _settingsManager.Settings.AffixAreaHeightOffsetBottom;
            set
            {
                _settingsManager.Settings.AffixAreaHeightOffsetBottom = value;
                RaisePropertyChanged(nameof(AffixAreaHeightOffsetBottom));

                _settingsManager.SaveSettings();
            }
        }

        public int AffixAspectAreaWidthOffset
        {
            get => _settingsManager.Settings.AffixAspectAreaWidthOffset;
            set
            {
                _settingsManager.Settings.AffixAspectAreaWidthOffset = value;
                RaisePropertyChanged(nameof(AffixAspectAreaWidthOffset));

                _settingsManager.SaveSettings();
            }
        }

        public int AspectAreaHeightOffsetTop
        {
            get => _settingsManager.Settings.AspectAreaHeightOffsetTop;
            set
            {
                _settingsManager.Settings.AspectAreaHeightOffsetTop = value;
                RaisePropertyChanged(nameof(AspectAreaHeightOffsetTop));

                _settingsManager.SaveSettings();
            }
        }

        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }

        public OcrResult OcrResultAspect
        {
            get => _ocrResultAspect;
            set
            {
                _ocrResultAspect = value;
                RaisePropertyChanged(nameof(OcrResultAspect));
            }
        }

        public BitmapSource? ProcessedScreenItemTooltip
        {
            get => _processedScreenItemTooltip;
            set
            {
                _processedScreenItemTooltip = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemTooltip));
            }
        }

        public BitmapSource? ProcessedScreenItemType
        {
            get => _processedScreenItemType;
            set
            {
                _processedScreenItemType = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemType));
            }
        }

        public BitmapSource? ProcessedScreenItemAffixLocations
        {
            get => _processedScreenItemAffixLocations;
            set
            {
                _processedScreenItemAffixLocations = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemAffixLocations));
            }
        }

        public BitmapSource? ProcessedScreenItemAffixAreas
        {
            get => _processedScreenItemAffixAreas;
            set
            {
                _processedScreenItemAffixAreas = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemAffixAreas));
            }
        }

        public BitmapSource? ProcessedScreenItemAffixes
        {
            get => _processedScreenItemAffixes;
            set
            {
                _processedScreenItemAffixes = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemAffixes));
            }
        }

        public BitmapSource? ProcessedScreenItemAspectLocation
        {
            get => _processedScreenItemAspectLocation;
            set
            {
                _processedScreenItemAspectLocation = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemAspectLocation));
            }
        }

        public BitmapSource? ProcessedScreenItemAspectArea
        {
            get => _processedScreenItemAspectArea;
            set
            {
                _processedScreenItemAspectArea = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemAspectArea));
            }
        }

        public BitmapSource? ProcessedScreenItemAspect
        {
            get => _processedScreenItemAspect;
            set
            {
                _processedScreenItemAspect = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemAspect));
            }
        }

        public BitmapSource? ProcessedScreenItemSocketLocations
        {
            get => _processedScreenItemSocketLocations;
            set
            {
                _processedScreenItemSocketLocations = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemSocketLocations));
            }
        }

        public int ThresholdMin
        {
            get => _settingsManager.Settings.ThresholdMin;
            set
            {
                _settingsManager.Settings.ThresholdMin = value;
                RaisePropertyChanged(nameof(ThresholdMin));

                _settingsManager.SaveSettings();

                _eventAggregator.GetEvent<BrightnessThresholdChangedEvent>().Publish();
            }
        }

        public int ThresholdMax
        {
            get => _settingsManager.Settings.ThresholdMax;
            set
            {
                _settingsManager.Settings.ThresholdMax = value;
                RaisePropertyChanged(nameof(ThresholdMax));

                _settingsManager.SaveSettings();

                _eventAggregator.GetEvent<BrightnessThresholdChangedEvent>().Publish();
            }
        }

        public double ThresholdSimilarityTooltip
        {
            get => _settingsManager.Settings.ThresholdSimilarityTooltip;
            set
            {
                _settingsManager.Settings.ThresholdSimilarityTooltip = value;
                RaisePropertyChanged(nameof(ThresholdSimilarityTooltip));

                _settingsManager.SaveSettings();
            }
        }

        public double ThresholdSimilarityType
        {
            get => _settingsManager.Settings.ThresholdSimilarityType;
            set
            {
                _settingsManager.Settings.ThresholdSimilarityType = value;
                RaisePropertyChanged(nameof(ThresholdSimilarityType));

                _settingsManager.SaveSettings();
            }
        }

        public double ThresholdSimilarityAffixLocation
        {
            get => _settingsManager.Settings.ThresholdSimilarityAffixLocation;
            set
            {
                _settingsManager.Settings.ThresholdSimilarityAffixLocation = value;
                RaisePropertyChanged(nameof(ThresholdSimilarityAffixLocation));

                _settingsManager.SaveSettings();
            }
        }

        public double ThresholdSimilarityAffix
        {
            get => _settingsManager.Settings.ThresholdSimilarityAffix;
            set
            {
                _settingsManager.Settings.ThresholdSimilarityAffix = value;
                RaisePropertyChanged(nameof(ThresholdSimilarityAffix));

                _settingsManager.SaveSettings();
            }
        }

        public double ThresholdSimilarityAspectLocation
        {
            get => _settingsManager.Settings.ThresholdSimilarityAspectLocation;
            set
            {
                _settingsManager.Settings.ThresholdSimilarityAspectLocation = value;
                RaisePropertyChanged(nameof(ThresholdSimilarityAspectLocation));

                _settingsManager.SaveSettings();
            }
        }

        public double ThresholdSimilarityAspect
        {
            get => _settingsManager.Settings.ThresholdSimilarityAspect;
            set
            {
                _settingsManager.Settings.ThresholdSimilarityAspect = value;
                RaisePropertyChanged(nameof(ThresholdSimilarityAspect));

                _settingsManager.SaveSettings();
            }
        }

        public double ThresholdSimilaritySocketLocation
        {
            get => _settingsManager.Settings.ThresholdSimilaritySocketLocation;
            set
            {
                _settingsManager.Settings.ThresholdSimilaritySocketLocation = value;
                RaisePropertyChanged(nameof(ThresholdSimilaritySocketLocation));

                _settingsManager.SaveSettings();
            }
        }

        public int TooltipWidth
        {
            get => _settingsManager.Settings.TooltipWidth;
            set
            {
                _settingsManager.Settings.TooltipWidth = value;
                RaisePropertyChanged(nameof(TooltipWidth));

                _settingsManager.SaveSettings();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleScreenProcessItemTooltipReadyEvent(ScreenProcessItemTooltipReadyEventParams screenProcessItemTooltipReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemTooltip = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemTooltipReadyEventParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemTypeReadyEvent(ScreenProcessItemTypeReadyEventParams screenProcessItemTypeReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemType = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemTypeReadyEventParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAffixLocationsReadyEvent(ScreenProcessItemAffixLocationsReadyEventParams screenProcessItemAffixLocationsReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemAffixLocations = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemAffixLocationsReadyEventParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAffixAreasReadyEvent(ScreenProcessItemAffixAreasReadyEventParams screenProcessItemAffixAreasReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemAffixAreas = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemAffixAreasReadyEventParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAffixesReadyEvent(ScreenProcessItemAffixesReadyEventParams screenProcessItemAffixesReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemAffixes = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemAffixesReadyEventParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAffixesOcrReadyEvent(ScreenProcessItemAffixesOcrReadyEventParams screenProcessItemAffixesOcrReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                OcrResultAffixes.Clear();
                OcrResultAffixes.AddRange(screenProcessItemAffixesOcrReadyEventParams.OcrResults);
            });
        }

        private void HandleScreenProcessItemAspectLocationReadyEvent(ScreenProcessItemAspectLocationReadyEventParams screenProcessItemAspectLocationReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemAspectLocation = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemAspectLocationReadyEventParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAspectAreaReadyEvent(ScreenProcessItemAspectAreaReadyEventParams screenProcessItemAspectAreaReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemAspectArea = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemAspectAreaReadyEventParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAspectReadyEvent(ScreenProcessItemAspectReadyEventParams screenProcessItemAspectReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemAspect = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemAspectReadyEventParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAspectOcrReadyEvent(ScreenProcessItemAspectOcrReadyEventParams screenProcessItemAspectOcrReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                OcrResultAspect = screenProcessItemAspectOcrReadyEventParams.OcrResult;
            });
        }

        private void HandleScreenProcessItemSocketLocationsReadyEvent(ScreenProcessItemSocketLocationsReadyEventParams screenProcessItemSocketLocationsReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemSocketLocations = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemSocketLocationsReadyEventParams.ProcessedScreen);
            });
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
