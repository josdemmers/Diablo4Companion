using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Extensions;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using D4Companion.Messages;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace D4Companion.ViewModels
{
    public class DebugViewModel : ObservableObject
    {
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

        public DebugViewModel(ILogger<DebugViewModel> logger, ISettingsManager settingsManager)
        {
            // Init services
            _logger = logger;
            _settingsManager = settingsManager;

            // Init messages
            WeakReferenceMessenger.Default.Register<ScreenProcessItemTooltipReadyMessage>(this, HandleScreenProcessItemTooltipReadyMessage);
            WeakReferenceMessenger.Default.Register<ScreenProcessItemTypeReadyMessage>(this, HandleScreenProcessItemTypeReadyMessage);
            WeakReferenceMessenger.Default.Register<ScreenProcessItemAffixLocationsReadyMessage>(this, HandleScreenProcessItemAffixLocationsReadyMessage);
            WeakReferenceMessenger.Default.Register<ScreenProcessItemAffixAreasReadyMessage>(this, HandleScreenProcessItemAffixAreasReadyMessage);
            WeakReferenceMessenger.Default.Register<ScreenProcessItemAffixesOcrReadyMessage>(this, HandleScreenProcessItemAffixesOcrReadyMessage);
            WeakReferenceMessenger.Default.Register<ScreenProcessItemAspectLocationReadyMessage>(this, HandleScreenProcessItemAspectLocationReadyMessage);
            WeakReferenceMessenger.Default.Register<ScreenProcessItemAspectAreaReadyMessage>(this, HandleScreenProcessItemAspectAreaReadyMessage);
            WeakReferenceMessenger.Default.Register<ScreenProcessItemAspectOcrReadyMessage>(this, HandleScreenProcessItemAspectOcrReadyMessage);
            WeakReferenceMessenger.Default.Register<ScreenProcessItemSocketLocationsReadyMessage>(this, HandleScreenProcessItemSocketLocationsReadyMessage);
            WeakReferenceMessenger.Default.Register<ScreenProcessItemSplitterLocationsReadyMessage>(this, HandleScreenProcessItemSplitterLocationsReadyMessage);
            WeakReferenceMessenger.Default.Register<ScreenProcessItemTypePowerOcrReadyMessage>(this, HandleScreenProcessItemTypePowerOcrReadyMessage);
            WeakReferenceMessenger.Default.Register<SystemPresetChangedMessage>(this, HandleSystemPresetChangedMessage);
            WeakReferenceMessenger.Default.Register<TooltipDataReadyMessage>(this, HandleTooltipDataReadyMessage);

            // Init view commands
            ExportDebugImagesCommand = new RelayCommand(ExportDebugImagesExecute);
            ReloadSystemPresetImagesCommand = new RelayCommand(ReloadSystemPresetImagesExecute);
            ResetPerformceResultsCommand = new RelayCommand(ResetPerformceResultsExecute);
            TakeScreenshotCommand = new RelayCommand(TakeScreenshotExecute);

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

        public ICommand ExportDebugImagesCommand { get; }
        public ICommand ReloadSystemPresetImagesCommand { get; }
        public ICommand ResetPerformceResultsCommand { get; }
        public ICommand TakeScreenshotCommand { get; }

        public int AffixAreaHeightOffsetTop
        {
            get => _settingsManager.Settings.AffixAreaHeightOffsetTop;
            set
            {
                _settingsManager.Settings.AffixAreaHeightOffsetTop = value;
                OnPropertyChanged(nameof(AffixAreaHeightOffsetTop));

                _settingsManager.SaveSettings();
            }
        }

        public int AffixAreaHeightOffsetBottom
        {
            get => _settingsManager.Settings.AffixAreaHeightOffsetBottom;
            set
            {
                _settingsManager.Settings.AffixAreaHeightOffsetBottom = value;
                OnPropertyChanged(nameof(AffixAreaHeightOffsetBottom));

                _settingsManager.SaveSettings();
            }
        }

        public int AffixAspectAreaWidthOffset
        {
            get => _settingsManager.Settings.AffixAspectAreaWidthOffset;
            set
            {
                _settingsManager.Settings.AffixAspectAreaWidthOffset = value;
                OnPropertyChanged(nameof(AffixAspectAreaWidthOffset));

                _settingsManager.SaveSettings();
            }
        }

        public int AspectAreaHeightOffsetTop
        {
            get => _settingsManager.Settings.AspectAreaHeightOffsetTop;
            set
            {
                _settingsManager.Settings.AspectAreaHeightOffsetTop = value;
                OnPropertyChanged(nameof(AspectAreaHeightOffsetTop));

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
                OnPropertyChanged(nameof(IsDebugInfoEnabled));

                _settingsManager.SaveSettings();
            }
        }

        public bool IsTopMostEnabled
        {
            get => _settingsManager.Settings.IsTopMost;
            set
            {
                _settingsManager.Settings.IsTopMost = value;
                OnPropertyChanged(nameof(IsTopMostEnabled));

                _settingsManager.SaveSettings();

                WeakReferenceMessenger.Default.Send(new TopMostStateChangedMessage());
            }
        }

        public int MinimalOcrMatchType
        {
            get => _settingsManager.Settings.MinimalOcrMatchType;
            set
            {
                _settingsManager.Settings.MinimalOcrMatchType = value;
                OnPropertyChanged(nameof(MinimalOcrMatchType));

                _settingsManager.SaveSettings();
            }
        }

        public OcrResultAffix OcrResultAspect
        {
            get => _ocrResultAspect;
            set
            {
                _ocrResultAspect = value;
                OnPropertyChanged(nameof(OcrResultAspect));
            }
        }

        public OcrResultItemType OcrResultItemType
        {
            get => _ocrResultItemType;
            set
            {
                _ocrResultItemType = value;
                OnPropertyChanged(nameof(OcrResultItemType));
            }
        }

        public OcrResult OcrResultPower
        {
            get => _ocrResultPower;
            set
            {
                _ocrResultPower = value;
                OnPropertyChanged(nameof(OcrResultPower));
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
                OnPropertyChanged(nameof(ProcessedScreenItemTooltip));
            }
        }

        public BitmapSource? ProcessedScreenItemType
        {
            get => _processedScreenItemType;
            set
            {
                _processedScreenItemType = value;
                OnPropertyChanged(nameof(ProcessedScreenItemType));
            }
        }

        public BitmapSource? ProcessedScreenItemAffixLocations
        {
            get => _processedScreenItemAffixLocations;
            set
            {
                _processedScreenItemAffixLocations = value;
                OnPropertyChanged(nameof(ProcessedScreenItemAffixLocations));
            }
        }

        public BitmapSource? ProcessedScreenItemAffixAreas
        {
            get => _processedScreenItemAffixAreas;
            set
            {
                _processedScreenItemAffixAreas = value;
                OnPropertyChanged(nameof(ProcessedScreenItemAffixAreas));
            }
        }

        public BitmapSource? ProcessedScreenItemAspectLocation
        {
            get => _processedScreenItemAspectLocation;
            set
            {
                _processedScreenItemAspectLocation = value;
                OnPropertyChanged(nameof(ProcessedScreenItemAspectLocation));
            }
        }

        public BitmapSource? ProcessedScreenItemAspectArea
        {
            get => _processedScreenItemAspectArea;
            set
            {
                _processedScreenItemAspectArea = value;
                OnPropertyChanged(nameof(ProcessedScreenItemAspectArea));
            }
        }

        public BitmapSource? ProcessedScreenItemSocketLocations
        {
            get => _processedScreenItemSocketLocations;
            set
            {
                _processedScreenItemSocketLocations = value;
                OnPropertyChanged(nameof(ProcessedScreenItemSocketLocations));
            }
        }

        public BitmapSource? ProcessedScreenItemSplitterLocations
        {
            get => _processedScreenItemSplitterLocations;
            set
            {
                _processedScreenItemSplitterLocations = value;
                OnPropertyChanged(nameof(ProcessedScreenItemSplitterLocations));
            }
        }

        public int ThresholdMin
        {
            get => _settingsManager.Settings.ThresholdMin;
            set
            {
                _settingsManager.Settings.ThresholdMin = value;
                OnPropertyChanged(nameof(ThresholdMin));

                _settingsManager.SaveSettings();

                WeakReferenceMessenger.Default.Send(new BrightnessThresholdChangedMessage());
            }
        }

        public int ThresholdMax
        {
            get => _settingsManager.Settings.ThresholdMax;
            set
            {
                _settingsManager.Settings.ThresholdMax = value;
                OnPropertyChanged(nameof(ThresholdMax));

                _settingsManager.SaveSettings();

                WeakReferenceMessenger.Default.Send(new BrightnessThresholdChangedMessage());
            }
        }

        public double ThresholdSimilarityTooltip
        {
            get => _settingsManager.Settings.ThresholdSimilarityTooltip;
            set
            {
                _settingsManager.Settings.ThresholdSimilarityTooltip = value;
                OnPropertyChanged(nameof(ThresholdSimilarityTooltip));

                _settingsManager.SaveSettings();
            }
        }

        public double ThresholdSimilarityAffixLocation
        {
            get => _settingsManager.Settings.ThresholdSimilarityAffixLocation;
            set
            {
                _settingsManager.Settings.ThresholdSimilarityAffixLocation = value;
                OnPropertyChanged(nameof(ThresholdSimilarityAffixLocation));

                _settingsManager.SaveSettings();
            }
        }

        public double ThresholdSimilarityAspectLocation
        {
            get => _settingsManager.Settings.ThresholdSimilarityAspectLocation;
            set
            {
                _settingsManager.Settings.ThresholdSimilarityAspectLocation = value;
                OnPropertyChanged(nameof(ThresholdSimilarityAspectLocation));

                _settingsManager.SaveSettings();
            }
        }

        public double ThresholdSimilaritySocketLocation
        {
            get => _settingsManager.Settings.ThresholdSimilaritySocketLocation;
            set
            {
                _settingsManager.Settings.ThresholdSimilaritySocketLocation = value;
                OnPropertyChanged(nameof(ThresholdSimilaritySocketLocation));

                _settingsManager.SaveSettings();
            }
        }

        public double ThresholdSimilaritySplitterLocation
        {
            get => _settingsManager.Settings.ThresholdSimilaritySplitterLocation;
            set
            {
                _settingsManager.Settings.ThresholdSimilaritySplitterLocation = value;
                OnPropertyChanged(nameof(ThresholdSimilaritySplitterLocation));

                _settingsManager.SaveSettings();
            }
        }

        public int TooltipMaxHeight
        {
            get => _settingsManager.Settings.TooltipMaxHeight;
            set
            {
                _settingsManager.Settings.TooltipMaxHeight = value;
                OnPropertyChanged(nameof(TooltipMaxHeight));

                _settingsManager.SaveSettings();
            }
        }

        public int TooltipWidth
        {
            get => _settingsManager.Settings.TooltipWidth;
            set
            {
                _settingsManager.Settings.TooltipWidth = value;
                OnPropertyChanged(nameof(TooltipWidth));

                _settingsManager.SaveSettings();
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleScreenProcessItemTooltipReadyMessage(object recipient, ScreenProcessItemTooltipReadyMessage message)
        {
            var screenProcessItemTooltipReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemTooltip = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemTooltipReadyMessageParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemTypeReadyMessage(object recipient, ScreenProcessItemTypeReadyMessage message)
        {
            var screenProcessItemTypeReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemType = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemTypeReadyMessageParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAffixLocationsReadyMessage(object recipient, ScreenProcessItemAffixLocationsReadyMessage message)
        {
            var screenProcessItemAffixLocationsReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemAffixLocations = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemAffixLocationsReadyMessageParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAffixAreasReadyMessage(object recipient, ScreenProcessItemAffixAreasReadyMessage message)
        {
            var screenProcessItemAffixAreasReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemAffixAreas = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemAffixAreasReadyMessageParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAffixesOcrReadyMessage(object recipient, ScreenProcessItemAffixesOcrReadyMessage message)
        {
            var screenProcessItemAffixesOcrReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                OcrResultAffixes.Clear();
                OcrResultAffixes.AddRange(screenProcessItemAffixesOcrReadyMessageParams.OcrResults);
            });
        }

        private void HandleScreenProcessItemAspectLocationReadyMessage(object recipient, ScreenProcessItemAspectLocationReadyMessage message)
        {
            var screenProcessItemAspectLocationReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemAspectLocation = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemAspectLocationReadyMessageParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAspectAreaReadyMessage(object recipient, ScreenProcessItemAspectAreaReadyMessage message)
        {
            var screenProcessItemAspectAreaReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemAspectArea = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemAspectAreaReadyMessageParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemAspectOcrReadyMessage(object recipient, ScreenProcessItemAspectOcrReadyMessage message)
        {
            var screenProcessItemAspectOcrReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                OcrResultAspect = screenProcessItemAspectOcrReadyMessageParams.OcrResult;
            });
        }

        private void HandleScreenProcessItemSocketLocationsReadyMessage(object recipient, ScreenProcessItemSocketLocationsReadyMessage message)
        {
            var screenProcessItemSocketLocationsReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemSocketLocations = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemSocketLocationsReadyMessageParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemSplitterLocationsReadyMessage(object recipient, ScreenProcessItemSplitterLocationsReadyMessage message)
        {
            var screenProcessItemSplitterLocationsReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                ProcessedScreenItemSplitterLocations = Helpers.ScreenCapture.ImageSourceFromBitmap(screenProcessItemSplitterLocationsReadyMessageParams.ProcessedScreen);
            });
        }

        private void HandleScreenProcessItemTypePowerOcrReadyMessage(object recipient, ScreenProcessItemTypePowerOcrReadyMessage message)
        {
            var screenProcessItemTypePowerOcrReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                OcrResultPower = screenProcessItemTypePowerOcrReadyMessageParams.OcrResultPower;
                OcrResultItemType = screenProcessItemTypePowerOcrReadyMessageParams.OcrResultItemType;
            });
        }

        private void HandleSystemPresetChangedMessage(object recipient, SystemPresetChangedMessage message)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                OnPropertyChanged(nameof(AffixAreaHeightOffsetTop));
                OnPropertyChanged(nameof(AffixAreaHeightOffsetBottom));
                OnPropertyChanged(nameof(AffixAspectAreaWidthOffset));
                OnPropertyChanged(nameof(AspectAreaHeightOffsetTop));
                OnPropertyChanged(nameof(ThresholdMin));
                OnPropertyChanged(nameof(ThresholdMax));
                OnPropertyChanged(nameof(TooltipWidth));
                OnPropertyChanged(nameof(TooltipMaxHeight));
            });
        }

        private void HandleTooltipDataReadyMessage(object recipient, TooltipDataReadyMessage message)
        {
            var tooltipDataReadyMessageParams = message.Value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                lock (_lockPerformanceResults)
                {
                    foreach (var performanceResult in tooltipDataReadyMessageParams.Tooltip.PerformanceResults)
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
                WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                {
                    Message = $"Failed to save debug images."
                }));
            }
        }

        private void ReloadSystemPresetImagesExecute()
        {
            WeakReferenceMessenger.Default.Send(new AvailableImagesChangedMessage());
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
            WeakReferenceMessenger.Default.Send(new TakeScreenshotRequestedMessage());
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
