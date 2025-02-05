using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

namespace D4Companion.ViewModels
{
    public class DebugViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private int? _badgeCount = null;
        private object _lockPerformanceResults = new();
        private OcrResultAffix _ocrResultAspect = new();
        private OcrResultItemType _ocrResultItemType = new();
        private OcrResult _ocrResultPower = new();

        private BitmapSource? _processedScreenItemTooltip = null;
        private BitmapSource? _processedScreenItemType = null;
        private BitmapSource? _processedScreenItemAffixLocations = null;
        private BitmapSource? _processedScreenItemAffixAreas = null;
        private BitmapSource? _processedScreenItemAspectLocation = null;
        private BitmapSource? _processedScreenItemAspectArea = null;
        private BitmapSource? _processedScreenItemSocketLocations = null;
        private BitmapSource? _processedScreenItemSplitterLocations = null;

        private Dictionary<string, ObservableCollection<ObservableValue>> _graphMappings = new();
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
            _eventAggregator.GetEvent<ScreenProcessItemAffixesOcrReadyEvent>().Subscribe(HandleScreenProcessItemAffixesOcrReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAspectLocationReadyEvent>().Subscribe(HandleScreenProcessItemAspectLocationReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAspectAreaReadyEvent>().Subscribe(HandleScreenProcessItemAspectAreaReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemAspectOcrReadyEvent>().Subscribe(HandleScreenProcessItemAspectOcrReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemSocketLocationsReadyEvent>().Subscribe(HandleScreenProcessItemSocketLocationsReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemSplitterLocationsReadyEvent>().Subscribe(HandleScreenProcessItemSplitterLocationsReadyEvent);
            _eventAggregator.GetEvent<ScreenProcessItemTypePowerOcrReadyEvent>().Subscribe(HandleScreenProcessItemTypePowerOcrReadyEvent);
            _eventAggregator.GetEvent<SystemPresetChangedEvent>().Subscribe(HandleSystemPresetChangedEvent);
            _eventAggregator.GetEvent<TooltipDataReadyEvent>().Subscribe(HandleTooltipDataReadyEvent);            

            // Init logger
            _logger = logger;

            // Init services
            _settingsManager = settingsManager;

            // Init View commands
            ExportDebugImagesCommand = new DelegateCommand(ExportDebugImagesExecute);
            ReloadSystemPresetImagesCommand = new DelegateCommand(ReloadSystemPresetImagesExecute);
            ResetPerformceResultsCommand = new DelegateCommand(ResetPerformceResultsExecute);
            TakeScreenshotCommand = new DelegateCommand(TakeScreenshotExecute);

            // Init
            InitGraph();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<OcrResultDescriptor> OcrResultAffixes { get => _ocrResultAffixes; set => _ocrResultAffixes = value; }
        public ObservableCollection<ISeries>? Series { get; set; } = new();

        public DelegateCommand ExportDebugImagesCommand { get; }
        public DelegateCommand ReloadSystemPresetImagesCommand { get; }
        public DelegateCommand ResetPerformceResultsCommand { get; }
        public DelegateCommand TakeScreenshotCommand { get; }


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

        public bool IsDebugInfoEnabled
        {
            get => _settingsManager.Settings.IsDebugInfoEnabled;
            set
            {
                _settingsManager.Settings.IsDebugInfoEnabled = value;
                RaisePropertyChanged(nameof(IsDebugInfoEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsTopMostEnabled
        {
            get => _settingsManager.Settings.IsTopMost;
            set
            {
                _settingsManager.Settings.IsTopMost = value;
                RaisePropertyChanged(nameof(IsTopMostEnabled));

                _settingsManager.SaveSettings();

                _eventAggregator.GetEvent<TopMostStateChangedEvent>().Publish();
            }
        }

        public int MinimalOcrMatchType
        {
            get => _settingsManager.Settings.MinimalOcrMatchType;
            set
            {
                _settingsManager.Settings.MinimalOcrMatchType = value;
                RaisePropertyChanged(nameof(MinimalOcrMatchType));

                _settingsManager.SaveSettings();
            }
        }

        public OcrResultAffix OcrResultAspect
        {
            get => _ocrResultAspect;
            set
            {
                _ocrResultAspect = value;
                RaisePropertyChanged(nameof(OcrResultAspect));
            }
        }

        public OcrResultItemType OcrResultItemType
        {
            get => _ocrResultItemType;
            set
            {
                _ocrResultItemType = value;
                RaisePropertyChanged(nameof(OcrResultItemType));
            }
        }

        public OcrResult OcrResultPower
        {
            get => _ocrResultPower;
            set
            {
                _ocrResultPower = value;
                RaisePropertyChanged(nameof(OcrResultPower));
            }
        }

        public LiveChartsCore.Measure.Margin? Margin { get; set; }
        public LiveChartsCore.SkiaSharpView.Axis[]? XAxes { get; set; }
        public LiveChartsCore.SkiaSharpView.Axis[]? YAxes { get; set; }

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

        public BitmapSource? ProcessedScreenItemSocketLocations
        {
            get => _processedScreenItemSocketLocations;
            set
            {
                _processedScreenItemSocketLocations = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemSocketLocations));
            }
        }

        public BitmapSource? ProcessedScreenItemSplitterLocations
        {
            get => _processedScreenItemSplitterLocations;
            set
            {
                _processedScreenItemSplitterLocations = value;
                RaisePropertyChanged(nameof(ProcessedScreenItemSplitterLocations));
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

        public double ThresholdSimilaritySplitterLocation
        {
            get => _settingsManager.Settings.ThresholdSimilaritySplitterLocation;
            set
            {
                _settingsManager.Settings.ThresholdSimilaritySplitterLocation = value;
                RaisePropertyChanged(nameof(ThresholdSimilaritySplitterLocation));

                _settingsManager.SaveSettings();
            }
        }

        public int TooltipMaxHeight
        {
            get => _settingsManager.Settings.TooltipMaxHeight;
            set
            {
                _settingsManager.Settings.TooltipMaxHeight = value;
                RaisePropertyChanged(nameof(TooltipMaxHeight));

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

        private void HandleScreenProcessItemSplitterLocationsReadyEvent(ScreenProcessItemSplitterLocationsReadyEventParams screenProcessItemSplitterLocationsReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemSplitterLocations = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemSplitterLocationsReadyEventParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemTypePowerOcrReadyEvent(ScreenProcessItemTypePowerOcrReadyEventParams screenProcessItemTypePowerOcrReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                OcrResultPower = screenProcessItemTypePowerOcrReadyEventParams.OcrResultPower;
                OcrResultItemType = screenProcessItemTypePowerOcrReadyEventParams.OcrResultItemType;
            });
        }

        private void HandleSystemPresetChangedEvent()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                RaisePropertyChanged(nameof(AffixAreaHeightOffsetTop));
                RaisePropertyChanged(nameof(AffixAreaHeightOffsetBottom));
                RaisePropertyChanged(nameof(AffixAspectAreaWidthOffset));
                RaisePropertyChanged(nameof(AspectAreaHeightOffsetTop));
                RaisePropertyChanged(nameof(ThresholdMin));
                RaisePropertyChanged(nameof(ThresholdMax));
                RaisePropertyChanged(nameof(TooltipWidth));
                RaisePropertyChanged(nameof(TooltipMaxHeight));
            });
        }

        private void HandleTooltipDataReadyEvent(TooltipDataReadyEventParams tooltipDataReadyEventParams)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                lock (_lockPerformanceResults)
                {
                    foreach (var performanceResult in tooltipDataReadyEventParams.Tooltip.PerformanceResults)
                    {
                        if (_graphMappings.TryGetValue(performanceResult.Key, out var series))
                        {
                            series.Add(new(performanceResult.Value));
                            if (series.Count > 100) series.RemoveAt(0);
                        }
                    }
                }
            });
        }

        private void ExportDebugImagesExecute()
        {
            try
            {
                int offset = 10;

                // Max height
                double maxHeight = Math.Max(_processedScreenItemTooltip?.Height ?? 0, _processedScreenItemType?.Height ?? 0);
                maxHeight = Math.Max(maxHeight, _processedScreenItemAffixLocations?.Height ?? 0);
                maxHeight = Math.Max(maxHeight, _processedScreenItemAffixAreas?.Height ?? 0);
                maxHeight = Math.Max(maxHeight, _processedScreenItemAspectLocation?.Height ?? 0);
                maxHeight = Math.Max(maxHeight, _processedScreenItemAspectArea?.Height ?? 0);
                maxHeight = Math.Max(maxHeight, _processedScreenItemSocketLocations?.Height ?? 0);
                maxHeight = Math.Max(maxHeight, _processedScreenItemSplitterLocations?.Height ?? 0);

                // Max width
                double maxWidth = Math.Max(_processedScreenItemTooltip?.Width ?? 0, _processedScreenItemType?.Width ?? 0);
                maxWidth = Math.Max(maxWidth, _processedScreenItemAffixLocations?.Width ?? 0);
                maxWidth = Math.Max(maxWidth, _processedScreenItemAffixAreas?.Width ?? 0);
                maxWidth = Math.Max(maxWidth, _processedScreenItemAspectLocation?.Width ?? 0);
                maxWidth = Math.Max(maxWidth, _processedScreenItemAspectArea?.Width ?? 0);
                maxWidth = Math.Max(maxWidth, _processedScreenItemSocketLocations?.Width ?? 0);
                maxWidth = Math.Max(maxWidth, _processedScreenItemSplitterLocations?.Width ?? 0);

                // Total width
                double totalWidth = _processedScreenItemTooltip?.Width != null ? _processedScreenItemTooltip.Width + offset : 0;
                totalWidth = _processedScreenItemType?.Width != null ? totalWidth + _processedScreenItemType.Width + offset : totalWidth + 0;
                totalWidth = _processedScreenItemAffixLocations?.Width != null ? totalWidth + _processedScreenItemAffixLocations.Width + offset : totalWidth + 0;
                totalWidth = _processedScreenItemAffixAreas?.Width != null ? totalWidth + _processedScreenItemAffixAreas.Width + offset : totalWidth + 0;
                totalWidth = _processedScreenItemAspectLocation?.Width != null ? totalWidth + _processedScreenItemAspectLocation.Width + offset : totalWidth + 0;
                totalWidth = _processedScreenItemAspectArea?.Width != null ? totalWidth + _processedScreenItemAspectArea.Width + offset : totalWidth + 0;
                totalWidth = _processedScreenItemSocketLocations?.Width != null ? totalWidth + _processedScreenItemSocketLocations.Width + offset : totalWidth + 0;
                totalWidth = _processedScreenItemSplitterLocations?.Width != null ? totalWidth + _processedScreenItemSplitterLocations.Width + offset : totalWidth + 0;

                if (maxHeight == 0 || maxWidth == 0 || totalWidth == 0)
                    return;

                // Create - horizontal
                Bitmap bitmap = new Bitmap((int)totalWidth, (int)maxHeight);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    int currentWidth = 0;
                    if (_processedScreenItemTooltip != null)
                    {
                        g.DrawImage(ScreenCapture.BitmapFromSource(_processedScreenItemTooltip), currentWidth, 0);
                        currentWidth = currentWidth + (int)(_processedScreenItemTooltip.Width + offset);
                    }
                    if (_processedScreenItemType != null)
                    {
                        g.DrawImage(ScreenCapture.BitmapFromSource(_processedScreenItemType), currentWidth, 0);
                        currentWidth = currentWidth + (int)(_processedScreenItemType.Width + offset);
                    }
                    if (_processedScreenItemAffixLocations != null)
                    {
                        g.DrawImage(ScreenCapture.BitmapFromSource(_processedScreenItemAffixLocations), currentWidth, 0);
                        currentWidth = currentWidth + (int)(_processedScreenItemAffixLocations.Width + offset);
                    }
                    if (_processedScreenItemAffixAreas != null)
                    {
                        g.DrawImage(ScreenCapture.BitmapFromSource(_processedScreenItemAffixAreas), currentWidth, 0);
                        currentWidth = currentWidth + (int)(_processedScreenItemAffixAreas.Width + offset);
                    }
                    if (_processedScreenItemAspectLocation != null)
                    {
                        g.DrawImage(ScreenCapture.BitmapFromSource(_processedScreenItemAspectLocation), currentWidth, 0);
                        currentWidth = currentWidth + (int)(_processedScreenItemAspectLocation.Width + offset);
                    }
                    if (_processedScreenItemAspectArea != null)
                    {
                        g.DrawImage(ScreenCapture.BitmapFromSource(_processedScreenItemAspectArea), currentWidth, 0);
                        currentWidth = currentWidth + (int)(_processedScreenItemAspectArea.Width + offset);
                    }
                    if (_processedScreenItemSocketLocations != null)
                    {
                        g.DrawImage(ScreenCapture.BitmapFromSource(_processedScreenItemSocketLocations), currentWidth, 0);
                        currentWidth = currentWidth + (int)(_processedScreenItemSocketLocations.Width + offset);
                    }
                    if (_processedScreenItemSplitterLocations != null)
                    {
                        g.DrawImage(ScreenCapture.BitmapFromSource(_processedScreenItemSplitterLocations), currentWidth, 0);
                        currentWidth = currentWidth + (int)(_processedScreenItemSplitterLocations.Width + offset);
                    }
                }

                string fileName = $"Screenshots/debug_{_settingsManager.Settings.SelectedSystemPreset}_{DateTime.Now.ToFileTimeUtc()}.png";
                string path = Path.GetDirectoryName(fileName) ?? string.Empty;
                ScreenCapture.WriteBitmapToFile(fileName, bitmap);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"Failed to save debug images."
                });
            }
        }

        private void ReloadSystemPresetImagesExecute()
        {
            _eventAggregator.GetEvent<AvailableImagesChangedEvent>().Publish();
        }

        private void ResetPerformceResultsExecute()
        {
            lock(_lockPerformanceResults)
            {
                foreach (var graphSeries in _graphMappings)
                {
                    graphSeries.Value.Clear();
                }
            }
        }

        private void TakeScreenshotExecute()
        {
            _eventAggregator.GetEvent<TakeScreenshotRequestedEvent>().Publish();
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitGraph()
        {
            _graphMappings.Add("Total", new ObservableCollection<ObservableValue>());
            _graphMappings.Add("Tooltip", new ObservableCollection<ObservableValue>());
            _graphMappings.Add("ItemTypePower", new ObservableCollection<ObservableValue>());
            _graphMappings.Add("AffixLocations", new ObservableCollection<ObservableValue>());
            _graphMappings.Add("AspectLocations", new ObservableCollection<ObservableValue>());
            _graphMappings.Add("SocketLocations", new ObservableCollection<ObservableValue>());
            _graphMappings.Add("SplitterLocations", new ObservableCollection<ObservableValue>());
            _graphMappings.Add("AffixAreas", new ObservableCollection<ObservableValue>());
            _graphMappings.Add("AspectAreas", new ObservableCollection<ObservableValue>());
            _graphMappings.Add("Affixes", new ObservableCollection<ObservableValue>());
            _graphMappings.Add("Aspects", new ObservableCollection<ObservableValue>());

            void AddSeries(string name, ObservableCollection<ObservableValue> values)
            {
                Series?.Add(new LineSeries<ObservableValue>
                {
                    Name = name,
                    Values = values,
                    Fill = null,
                    GeometryStroke = null,
                    GeometryFill = null
                });
            }

            foreach (var graphMapping in _graphMappings)
            {
                AddSeries(graphMapping.Key, graphMapping.Value);
            }

            XAxes = new[] { new Axis { LabelsPaint = new SolidColorPaint(new SKColor(100, 100, 100, 100)) } };
            YAxes = new[] { new Axis { LabelsPaint = new SolidColorPaint(new SKColor(100, 100, 100, 100)) } };

            var auto = LiveChartsCore.Measure.Margin.Auto;
            Margin = new(50, auto, 50, auto);
        }

        #endregion
    }
}
