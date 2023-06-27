using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;

namespace D4Companion.Services
{
    public class ScreenProcessHandler : IScreenProcessHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;
        private readonly IAffixPresetManager _affixPresetManager;

        private Image<Bgr, byte> _currentScreen = new Image<Bgr, byte>(0, 0);
        private ItemTooltipDescriptor _currentTooltip = new ItemTooltipDescriptor();
        Dictionary<string, Image<Bgr, byte>> _imageListTooltips = new Dictionary<string, Image<Bgr, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemTypes = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixes = new Dictionary<string, Image<Gray, byte>>();
        private bool _isEnabled = false;
        private object lockHandleScreenCaptureReadyEvent = new object();
        private Task? _processTask = null;

        // Start of Constructors region

        #region Constructors

        public ScreenProcessHandler(IEventAggregator eventAggregator, ILogger<ScreenProcessHandler> logger, ISettingsManager settingsManager, IAffixPresetManager affixPresetManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ScreenCaptureReadyEvent>().Subscribe(HandleScreenCaptureReadyEvent);
            _eventAggregator.GetEvent<SystemPresetChangedEvent>().Subscribe(HandleSystemPresetChangedEvent);
            _eventAggregator.GetEvent<ToggleOverlayEvent>().Subscribe(HandleToggleOverlayEvent);
            _eventAggregator.GetEvent<ToggleOverlayFromGUIEvent>().Subscribe(HandleToggleOverlayFromGUIEvent);

            // Init logger
            _logger = logger;

            // Init services
            _affixPresetManager = affixPresetManager;
            _settingsManager = settingsManager;

            // Init image list.
            initImageList();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleScreenCaptureReadyEvent(ScreenCaptureReadyEventParams screenCaptureReadyEventParams)
        {
            lock (lockHandleScreenCaptureReadyEvent)
            {
                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Processing task state: {_processTask?.Status}");

                if (!IsEnabled) return;
                if (_processTask != null && (_processTask.Status.Equals(TaskStatus.Running) || _processTask.Status.Equals(TaskStatus.WaitingForActivation))) return;
                if (screenCaptureReadyEventParams.CurrentScreen == null) return;

                _currentScreen = screenCaptureReadyEventParams.CurrentScreen.ToImage<Bgr, byte>();
                _processTask = StartProcessTask();
            }
        }

        private void HandleSystemPresetChangedEvent()
        {
            initImageList();
        }

        private void HandleToggleOverlayEvent(ToggleOverlayEventParams toggleOverlayEventParams)
        {
            IsEnabled = toggleOverlayEventParams.IsEnabled;
        }

        private void HandleToggleOverlayFromGUIEvent(ToggleOverlayFromGUIEventParams toggleOverlayFromGUIEventParams)
        {
            IsEnabled = toggleOverlayFromGUIEventParams.IsEnabled;
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void initImageList()
        {
            _imageListTooltips.Clear();
            _imageListItemTypes.Clear();
            _imageListItemAffixLocations.Clear();
            _imageListItemAffixes.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;

            // Tooltips
            string directory = $"Images\\{systemPreset}\\Tooltips\\";
            if (Directory.Exists(directory))
            {
                string[] fileEntries = Directory.GetFiles(directory);
                foreach (string fileName in fileEntries)
                {
                    _imageListTooltips.TryAdd(Path.GetFileNameWithoutExtension(fileName), new Image<Bgr, byte>(fileName));
                }
            }

            // Item types
            directory = $"Images\\{systemPreset}\\Types\\";
            if (Directory.Exists(directory))
            {
                string[] fileEntries = Directory.GetFiles(directory);
                foreach (string fileName in fileEntries)
                {
                    _imageListItemTypes.TryAdd(Path.GetFileNameWithoutExtension(fileName), new Image<Gray, byte>(fileName));
                }
            }

            // Item affix locations
            _imageListItemAffixLocations.TryAdd("dot", new Image<Gray, byte>($"Images\\{systemPreset}\\dot.png"));

            // Item affixes
            directory = $"Images\\{systemPreset}\\Affixes\\";
            if (Directory.Exists(directory))
            {
                string[] fileEntries = Directory.GetFiles(directory);
                foreach (string fileName in fileEntries)
                {
                    _imageListItemAffixes.TryAdd(Path.GetFileNameWithoutExtension(fileName), new Image<Gray, byte>(fileName));
                }
            }
        }

        private async Task StartProcessTask()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Clear previous tooltip
                    _currentTooltip = new ItemTooltipDescriptor();

                    if (_currentScreen.Height < 50)
                    {
                        _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Diablo IV window is probably minimized.");
                        return;
                    }

                    bool result = FindTooltips();
                    if (result) 
                    {
                        result = FindItemTypes();
                    }
                    if (result)
                    {
                        result = FindItemAffixLocations();
                    }
                    if (result)
                    {
                        result = FindItemAffixes();
                    }

                    _eventAggregator.GetEvent<TooltipDataReadyEvent>().Publish(new TooltipDataReadyEventParams
                    {
                        Tooltip = _currentTooltip
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item tooltip is found.</returns>
        private bool FindTooltips()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();

            // Convert the image to grayscale and apply threshold
            Image<Gray, byte> currentScreenFilter = new Image<Gray, byte>(currentScreen.Width, currentScreen.Height, new Gray(0));
            currentScreenFilter = currentScreen.Convert<Gray, byte>();
            //currentScreenFilter = currentScreenFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

            ConcurrentBag<ItemTooltipDescriptor> tooltipBag = new ConcurrentBag<ItemTooltipDescriptor>();
            Parallel.ForEach(_imageListTooltips.Keys, itemTooltip =>
            {
                tooltipBag.Add(FindTooltip(currentScreenFilter, _imageListTooltips[itemTooltip].Clone()));
            });

            // Sort results by accuracy
            var tooltips = tooltipBag.ToList();
            tooltips.Sort((x, y) =>
            {
                return x.Accuracy < y.Accuracy ? -1 : x.Accuracy > y.Accuracy ? 1 : 0;
            });

            // Only support one tooltip for now.
            foreach (var tooltip in tooltips)
            {
                if (tooltip.Location.IsEmpty) continue;

                _currentTooltip.Location = tooltip.Location;
                
                Point[] points = new Point[]
                {
                    new Point(tooltip.Location.Left,tooltip.Location.Bottom),
                    new Point(tooltip.Location.Right,tooltip.Location.Bottom),
                    new Point(tooltip.Location.Right,tooltip.Location.Top),
                    new Point(tooltip.Location.Left,tooltip.Location.Top)
                };
                CvInvoke.Polylines(currentScreen, points, true, new MCvScalar(0, 0, 255), 5);

                break;
            }

            //currentScreen.Save($"Logging/ScreenProcess.png");
            _eventAggregator.GetEvent<ScreenProcessItemTooltipReadyEvent>().Publish(new ScreenProcessItemTooltipReadyEventParams
            {
                ProcessedScreen = currentScreen.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !_currentTooltip.Location.IsEmpty;
        }

        private ItemTooltipDescriptor FindTooltip(Image<Gray, byte> currentScreen, Image<Bgr, byte> tooltip)
        {
            // Template-based Image Matching

            // Initialization
            ItemTooltipDescriptor tooltipDescriptor = new ItemTooltipDescriptor();
            Mat result = new Mat();

            // Convert the image to grayscale and apply threshold
            Image<Gray, byte> tooltipFilter = new Image<Gray, byte>(tooltip.Width, tooltip.Height, new Gray(0));
            tooltipFilter = tooltip.Convert<Gray, byte>();
            //tooltipFilter = tooltipFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

            CvInvoke.MatchTemplate(currentScreen, tooltipFilter, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);

            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Match value: {minVal}");
            //if (minVal < 0.005)
            //if (minVal < 0.01)
            if (minVal < 0.05)
            //if (minVal < 0.1)
            {
                tooltipDescriptor.Accuracy = minVal;
                tooltipDescriptor.Location = new Rectangle(minLoc, new Size(500, currentScreen.Height - minLoc.Y));
            }

            return tooltipDescriptor;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item type is found.</returns>
        private bool FindItemTypes()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();
            currentScreen.ROI = _currentTooltip.Location;
            var currentScreenRoi = currentScreen.Copy();
            currentScreen.ROI = Rectangle.Empty;

            // Convert the image to grayscale and apply threshold
            Image<Gray, byte> currentScreenRoiFilter = new Image<Gray, byte>(currentScreenRoi.Width, currentScreenRoi.Height, new Gray(0));
            currentScreenRoiFilter = currentScreenRoi.Convert<Gray, byte>();
            currentScreenRoiFilter = currentScreenRoiFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin),new Gray(_settingsManager.Settings.ThresholdMax));
            // Convert the image to color for GUI
            Image<Bgr, byte> currentScreenRoiFilterColor = new Image<Bgr, byte>(currentScreenRoiFilter.Width, currentScreenRoiFilter.Height, new Bgr());
            currentScreenRoiFilterColor = currentScreenRoiFilter.Convert<Bgr, byte>();

            ConcurrentBag<ItemTypeDescriptor> itemTypeBag = new ConcurrentBag<ItemTypeDescriptor>();
            Parallel.ForEach(_imageListItemTypes.Keys, itemType =>
            {
                itemTypeBag.Add(FindItemType(currentScreenRoiFilter, itemType));
            });

            // Sort results by accuracy
            var itemTypes = itemTypeBag.ToList();
            itemTypes.Sort((x, y) =>
            {
                return x.Accuracy < y.Accuracy ? -1 : x.Accuracy > y.Accuracy ? 1 : 0;
            });

            // Only support one tooltip for now.
            foreach ( var itemType in itemTypes) 
            {
                if (itemType.Location.IsEmpty) continue;

                _currentTooltip.ItemType = itemType.Name;

                Point[] points = new Point[]
                {
                    new Point(itemType.Location.Left,itemType.Location.Bottom),
                    new Point(itemType.Location.Right,itemType.Location.Bottom),
                    new Point(itemType.Location.Right,itemType.Location.Top),
                    new Point(itemType.Location.Left,itemType.Location.Top)
                };

                CvInvoke.Polylines(currentScreenRoiFilterColor, points, true, new MCvScalar(0, 0, 255), 5);

                break;
            }

            //currentScreenRoiFilterColor.Save($"Logging/ScreenProcess.png");
            _eventAggregator.GetEvent<ScreenProcessItemTypeReadyEvent>().Publish(new ScreenProcessItemTypeReadyEventParams
            {
                ProcessedScreen = currentScreenRoiFilterColor.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !string.IsNullOrWhiteSpace(_currentTooltip.ItemType);
        }

        private ItemTypeDescriptor FindItemType(Image<Gray, byte> currentTooltip, string currentItemType)
        {
            // Template-based Image Matching

            // Initialization
            ItemTypeDescriptor itemType = new ItemTypeDescriptor { Name = currentItemType };
            Mat result = new Mat();
            var currentItemTypeImage = _imageListItemTypes[currentItemType].Clone();

            currentItemTypeImage = currentItemTypeImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            CvInvoke.MatchTemplate(currentTooltip, currentItemTypeImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);

            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemType}) Accuracy: {minVal}");
            //if (minVal < 0.005)
            //if (minVal < 0.01)
            if (minVal < 0.05)
            //if (minVal < 0.1)
            {
                itemType.Accuracy = minVal;
                itemType.Location = new Rectangle(minLoc, currentItemTypeImage.Size);
            }

            return itemType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item affixes are found.</returns>
        private bool FindItemAffixLocations()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();
            currentScreen.ROI = _currentTooltip.Location;
            var currentScreenRoi = currentScreen.Copy();
            currentScreen.ROI = Rectangle.Empty;

            // Convert the image to grayscale and apply threshold
            Image<Gray, byte> currentScreenRoiFilter = new Image<Gray, byte>(currentScreenRoi.Width, currentScreenRoi.Height, new Gray(0));
            currentScreenRoiFilter = currentScreenRoi.Convert<Gray, byte>();
            //currentScreenRoiFilter = currentScreenRoiFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            // Convert the image to color for GUI
            Image<Bgr, byte> currentScreenRoiFilterColor = new Image<Bgr, byte>(currentScreenRoiFilter.Width, currentScreenRoiFilter.Height, new Bgr());
            currentScreenRoiFilterColor = currentScreenRoiFilter.Convert<Bgr, byte>();

            var itemAffixLocations = FindItemAffixLocation(currentScreenRoiFilter, "dot");

            foreach (var itemAffixLocation in itemAffixLocations)
            {
                _currentTooltip.ItemAffixLocations.Add(itemAffixLocation.Location);

                Point[] points = new Point[]
                {
                    new Point(itemAffixLocation.Location.Left,itemAffixLocation.Location.Bottom),
                    new Point(itemAffixLocation.Location.Right,itemAffixLocation.Location.Bottom),
                    new Point(itemAffixLocation.Location.Right,itemAffixLocation.Location.Top),
                    new Point(itemAffixLocation.Location.Left,itemAffixLocation.Location.Top)
                };

                CvInvoke.Polylines(currentScreenRoiFilterColor, points, true, new MCvScalar(0, 0, 255), 5);
            }

            //currentScreenRoiFilterColor.Save($"Logging/ScreenProcess.png");
            _eventAggregator.GetEvent<ScreenProcessItemAffixLocationsReadyEvent>().Publish(new ScreenProcessItemAffixLocationsReadyEventParams
            {
                ProcessedScreen = currentScreenRoiFilterColor.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return itemAffixLocations.Any();
        }

        private List<ItemAffixLocationDescriptor> FindItemAffixLocation(Image<Gray, byte> currentTooltip, string currentItemAffixLocation)
        {
            // Template-based Image Matching

            // Initialization
            List<ItemAffixLocationDescriptor> itemAffixLocations = new List<ItemAffixLocationDescriptor>();
            Mat result = new Mat();
            var currentItemTypeImage = _imageListItemAffixLocations[currentItemAffixLocation].Clone();
            //currentItemTypeImage = currentItemTypeImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MatchTemplate(currentTooltip, currentItemTypeImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
            Mat resultNorm = new Mat();
            CvInvoke.Normalize(result, resultNorm, 0, 1, Emgu.CV.CvEnum.NormType.MinMax, Emgu.CV.CvEnum.DepthType.Cv64F);
            Matrix<double> matches = new Matrix<double>(resultNorm.Size);
            resultNorm.CopyTo(matches);

            do
            {
                CvInvoke.MinMaxLoc(matches, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Match value: {minVal}");

                itemAffixLocations.Add(new ItemAffixLocationDescriptor
                {
                    Accuracy = minVal,
                    Location = new Rectangle(minLoc, currentItemTypeImage.Size)
                });

                matches[minLoc.Y, minLoc.X] = 0.5;
                matches[maxLoc.Y, maxLoc.X] = 0.5;

                Point[] points = new Point[]
                {
                        new Point(itemAffixLocations.Last().Location.Left,itemAffixLocations.Last().Location.Bottom),
                        new Point(itemAffixLocations.Last().Location.Right,itemAffixLocations.Last().Location.Bottom),
                        new Point(itemAffixLocations.Last().Location.Right,itemAffixLocations.Last().Location.Top),
                        new Point(itemAffixLocations.Last().Location.Left,itemAffixLocations.Last().Location.Top)
                };
                CvInvoke.Polylines(currentTooltip, points, true, new MCvScalar(), 5);

            } while (minVal < 0.05);
            //} while (minVal < 0.005);
            //} while (minVal < 0.01);
            //} while (minVal < 0.1);

            return itemAffixLocations;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item affixes are found.</returns>
        private bool FindItemAffixes()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();
            currentScreen.ROI = _currentTooltip.Location;
            var currentScreenRoi = currentScreen.Copy();
            currentScreen.ROI = Rectangle.Empty;

            // Convert the image to grayscale and apply threshold
            Image<Gray, byte> currentScreenRoiFilter = new Image<Gray, byte>(currentScreenRoi.Width, currentScreenRoi.Height, new Gray(0));
            currentScreenRoiFilter = currentScreenRoi.Convert<Gray, byte>();
            currentScreenRoiFilter = currentScreenRoiFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            // Convert the image to color for GUI
            Image<Bgr, byte> currentScreenRoiFilterColor = new Image<Bgr, byte>(currentScreenRoiFilter.Width, currentScreenRoiFilter.Height, new Bgr());
            currentScreenRoiFilterColor = currentScreenRoiFilter.Convert<Bgr, byte>();

            string affixPreset = _settingsManager.Settings.SelectedAffixName;
            var itemAffixes = _affixPresetManager.AffixPresets.FirstOrDefault(s => s.Name == affixPreset)?.ItemAffixes;
            var itemAffixesPerType = itemAffixes?.FindAll(itemAffix => _currentTooltip.ItemType.StartsWith($"{itemAffix.Type}_"));
            if (itemAffixesPerType != null) 
            {
                ConcurrentBag<ItemAffixDescriptor> itemAffixBag = new ConcurrentBag<ItemAffixDescriptor>();
                Parallel.ForEach(itemAffixesPerType, itemAffix =>
                {
                    itemAffixBag.Add(FindItemAffix(currentScreenRoiFilter, Path.GetFileNameWithoutExtension(itemAffix.FileName)));
                });

                // Sort results by accuracy
                var itemAffixResults = itemAffixBag.ToList();
                itemAffixResults.Sort((x, y) =>
                {
                    return x.Accuracy < y.Accuracy ? -1 : x.Accuracy > y.Accuracy ? 1 : 0;
                });

                foreach (var itemAffix in itemAffixResults)
                {
                    if (itemAffix.Location.IsEmpty) continue;

                    _currentTooltip.ItemAffixes.Add(itemAffix.Location);

                    Point[] points = new Point[]
                    {
                        new Point(itemAffix.Location.Left,itemAffix.Location.Bottom),
                        new Point(itemAffix.Location.Right,itemAffix.Location.Bottom),
                        new Point(itemAffix.Location.Right,itemAffix.Location.Top),
                        new Point(itemAffix.Location.Left,itemAffix.Location.Top)
                    };

                    CvInvoke.Polylines(currentScreenRoiFilterColor, points, true, new MCvScalar(0, 0, 255), 5);
                }
            }

            //currentScreenRoiFilterColor.Save($"Logging/ScreenProcess.png");
            _eventAggregator.GetEvent<ScreenProcessItemAffixesReadyEvent>().Publish(new ScreenProcessItemAffixesReadyEventParams
            {
                ProcessedScreen = currentScreenRoiFilterColor.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return _currentTooltip.ItemAffixes.Any();
        }

        private ItemAffixDescriptor FindItemAffix(Image<Gray, byte> currentTooltip, string currentItemAffix)
        {
            // Template-based Image Matching

            // Initialization
            ItemAffixDescriptor itemAffix = new ItemAffixDescriptor();
            Mat result = new Mat();
            var currentItemTypeImage = _imageListItemAffixes[currentItemAffix].Clone();

            currentItemTypeImage = currentItemTypeImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            CvInvoke.MatchTemplate(currentTooltip, currentItemTypeImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);

            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Match value: {minVal}");
            //if (minVal < 0.005)
            //if (minVal < 0.01)
            if (minVal < 0.05)
            //if (minVal < 0.1)
            {
                itemAffix.Accuracy = minVal;
                itemAffix.Location = new Rectangle(minLoc, currentItemTypeImage.Size);
            }

            return itemAffix;
        }

        #endregion
    }
}
