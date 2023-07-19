using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Reflection;

namespace D4Companion.Services
{
    public class ScreenProcessHandler : IScreenProcessHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;
        private readonly IAffixPresetManager _affixPresetManager;

        private Image<Bgr, byte> _currentScreen = new Image<Bgr, byte>(0, 0);
        private Image<Bgr, byte> _currentScreenTooltip = new Image<Bgr, byte>(0, 0);
        private ItemTooltipDescriptor _currentTooltip = new ItemTooltipDescriptor();
        Dictionary<string, Image<Gray, byte>> _imageListItemTooltips = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemTypes = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemTypesLite = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixes = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAspectLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAspects = new Dictionary<string, Image<Gray, byte>>();
        private bool _isEnabled = false;
        //private object lockHandleScreenCaptureReadyEvent = new object();
        private object _lockCloneImage = new object();
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
            //lock (_lockHandleScreenCaptureReadyEvent)
            //if(Monitor.TryEnter(_lockHandleScreenCaptureReadyEvent))
            //{
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Processing task state: {_processTask?.Status}");

            if (!IsEnabled) return;
            if (_processTask != null && (_processTask.Status.Equals(TaskStatus.Running) || _processTask.Status.Equals(TaskStatus.WaitingForActivation))) return;
            if (screenCaptureReadyEventParams.CurrentScreen == null) return;

            _currentScreen = screenCaptureReadyEventParams.CurrentScreen.ToImage<Bgr, byte>();
            _processTask?.Dispose();
            _processTask = StartProcessTask();
            //}
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
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            _imageListItemTooltips.Clear();
            _imageListItemTypes.Clear();
            _imageListItemTypesLite.Clear();
            _imageListItemAffixLocations.Clear();
            _imageListItemAffixes.Clear();
            _imageListItemAspectLocations.Clear();
            _imageListItemAspects.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;
            string directory = $"Images\\{systemPreset}\\";
            if (!Directory.Exists(directory))
            {
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"System preset not found at \"{directory}\". Go to settings to select one."
                });
                return;
            }

            // Tooltips
            directory = $"Images\\{systemPreset}\\Tooltips\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(tooltip => tooltip.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                foreach (string fileName in fileEntries)
                {
                    _imageListItemTooltips.TryAdd(Path.GetFileNameWithoutExtension(fileName).ToLower(), new Image<Gray, byte>(fileName));
                }
            }

            // Item types
            directory = $"Images\\{systemPreset}\\Types\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(itemType => itemType.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !itemType.ToLower().Contains("weapon_all"));
                foreach (string fileName in fileEntries)
                {
                    _imageListItemTypes.TryAdd(Path.GetFileNameWithoutExtension(fileName).ToLower(), new Image<Gray, byte>(fileName));
                }
            }

            // Item types lite
            directory = $"Images\\{systemPreset}\\Types\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(itemType => itemType.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                (itemType.ToLower().Contains("weapon_all") || (!itemType.ToLower().Contains("weapon_") && !itemType.ToLower().Contains("ranged_") && !itemType.ToLower().Contains("offhand_focus"))));
                foreach (string fileName in fileEntries)
                {
                    _imageListItemTypesLite.TryAdd(Path.GetFileNameWithoutExtension(fileName).ToLower(), new Image<Gray, byte>(fileName));
                }
            }

            // Item affix locations
            directory = $"Images\\{systemPreset}\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(affixLoc => affixLoc.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                affixLoc.Contains("dot-affixes_", StringComparison.OrdinalIgnoreCase));
                foreach (string fileName in fileEntries)
                {
                    _imageListItemAffixLocations.TryAdd(Path.GetFileNameWithoutExtension(fileName).ToLower(), new Image<Gray, byte>(fileName));
                }
            }

            // Item affixes
            directory = $"Images\\{systemPreset}\\Affixes\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(affix => affix.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                foreach (string fileName in fileEntries)
                {
                    _imageListItemAffixes.TryAdd(Path.GetFileNameWithoutExtension(fileName).ToLower(), new Image<Gray, byte>(fileName));
                }
            }

            // Item aspect locations
            directory = $"Images\\{systemPreset}\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(aspectLoc => aspectLoc.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                aspectLoc.Contains("dot-aspects_", StringComparison.OrdinalIgnoreCase));
                foreach (string fileName in fileEntries)
                {
                    _imageListItemAspectLocations.TryAdd(Path.GetFileNameWithoutExtension(fileName).ToLower(), new Image<Gray, byte>(fileName));
                }
            }

            // Item aspects
            directory = $"Images\\{systemPreset}\\Aspects\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(aspect => aspect.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                foreach (string fileName in fileEntries)
                {
                    _imageListItemAspects.TryAdd(Path.GetFileNameWithoutExtension(fileName).ToLower(), new Image<Gray, byte>(fileName));
                }
            }
        }

        private async Task StartProcessTask()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

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
                        // Result does not matter, always continue.
                        FindItemAffixes();

                        result = FindItemAspectLocations();
                    }
                    if (result)
                    {
                        // Result does not matter, always continue.
                        FindItemAspects();
                    }

                    _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Tooltip data ready:");
                    _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Item type: {_currentTooltip.ItemType}");
                    _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Item affixes: {_currentTooltip.ItemAffixes.Count}");
                    _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Item aspect: {!_currentTooltip.ItemAspect.IsEmpty}");

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
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();

            // Convert the image to grayscale
            Image<Gray, byte> currentScreenFilter = new Image<Gray, byte>(currentScreen.Width, currentScreen.Height, new Gray(0));
            currentScreenFilter = currentScreen.Convert<Gray, byte>();
            //currentScreenFilter = currentScreenFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

            ConcurrentBag<ItemTooltipDescriptor> itemTooltipBag = new ConcurrentBag<ItemTooltipDescriptor>();
            Parallel.ForEach(_imageListItemTooltips.Keys, itemTooltip =>
            {
                try
                {
                    itemTooltipBag.Add(FindTooltip(currentScreenFilter, itemTooltip));
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"{MethodBase.GetCurrentMethod()?.Name}");
                }
            });

            // Sort results by similarity
            var itemTooltips = itemTooltipBag.ToList();
            itemTooltips.Sort((x, y) =>
            {
                return x.Similarity < y.Similarity ? -1 : x.Similarity > y.Similarity ? 1 : 0;
            });

            foreach (var itemTooltip in itemTooltips)
            {
                if (itemTooltip.Location.IsEmpty) continue;

                _currentTooltip.Location = itemTooltip.Location;

                Point[] points = new Point[]
                {
                    new Point(itemTooltip.Location.Left,itemTooltip.Location.Bottom),
                    new Point(itemTooltip.Location.Right,itemTooltip.Location.Bottom),
                    new Point(itemTooltip.Location.Right,itemTooltip.Location.Top),
                    new Point(itemTooltip.Location.Left,itemTooltip.Location.Top)
                };
                CvInvoke.Polylines(currentScreen, points, true, new MCvScalar(0, 0, 255), 5);

                // Skip foreach after the first valid tooltip is found.
                break;
            }

            _eventAggregator.GetEvent<ScreenProcessItemTooltipReadyEvent>().Publish(new ScreenProcessItemTooltipReadyEventParams
            {
                ProcessedScreen = currentScreen.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            var result = !_currentTooltip.Location.IsEmpty;
            if (result)
            {
                // Create ROI for current tooltip
                _currentScreen.ROI = _currentTooltip.Location;
                _currentScreenTooltip = _currentScreen.Copy();
                _currentScreen.ROI = Rectangle.Empty;
            }

            return result;
        }

        private ItemTooltipDescriptor FindTooltip(Image<Gray, byte> currentScreen, string currentItemTooltip)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            // Template-based Image Matching

            // Initialization
            ItemTooltipDescriptor tooltip = new ItemTooltipDescriptor();
            Mat result = new Mat();
            Image<Gray, byte> currentItemTooltipImage = new Image<Gray, byte>(0, 0);

            try
            {
                // Initialization
                lock (_lockCloneImage)
                {
                    currentItemTooltipImage = _imageListItemTooltips[currentItemTooltip].Clone();
                }

                //currentItemTooltipImage = currentItemTooltipImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

                double minVal = 0.0;
                double maxVal = 0.0;
                Point minLoc = new Point();
                Point maxLoc = new Point();

                CvInvoke.MatchTemplate(currentScreen, currentItemTooltipImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemTooltip}) Similarity: {String.Format("{0:0.0000000000}", minVal)} @ {minLoc.X},{minLoc.Y}");

                if (minVal < _settingsManager.Settings.ThresholdSimilarityTooltip)
                {
                    tooltip.Similarity = minVal;
                    tooltip.Location = new Rectangle(new Point(minLoc.X, 0), new Size(_settingsManager.Settings.TooltipWidth, minLoc.Y));
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return tooltip;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item type is found.</returns>
        private bool FindItemTypes()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Convert the image to grayscale and apply threshold
            Image<Gray, byte> currentScreenTooltipFilter = new Image<Gray, byte>(_currentScreenTooltip.Width, _currentScreenTooltip.Height, new Gray(0));
            currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>();
            currentScreenTooltipFilter = currentScreenTooltipFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin),new Gray(_settingsManager.Settings.ThresholdMax));
            // Create image for GUI
            var currentScreenTooltipGui = new Image<Bgr, byte>(currentScreenTooltipFilter.Width, currentScreenTooltipFilter.Height, new Bgr());
            currentScreenTooltipGui = currentScreenTooltipFilter.Convert<Bgr, byte>();

            ConcurrentBag<ItemTypeDescriptor> itemTypeBag = new ConcurrentBag<ItemTypeDescriptor>();
            var itemTypeKeys = _settingsManager.Settings.LiteMode ? _imageListItemTypesLite.Keys : _imageListItemTypes.Keys;
            Parallel.ForEach(itemTypeKeys, itemType =>
            {
                try
                {
                    itemTypeBag.Add(FindItemType(currentScreenTooltipFilter, itemType));
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"{MethodBase.GetCurrentMethod()?.Name}");
                }
            });

            // Sort results by similarity
            var itemTypes = itemTypeBag.ToList();
            itemTypes.Sort((x, y) =>
            {
                return x.Similarity < y.Similarity ? -1 : x.Similarity > y.Similarity ? 1 : 0;
            });

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

                CvInvoke.Polylines(currentScreenTooltipGui, points, true, new MCvScalar(0, 0, 255), 5);

                // Skip foreach after the first valid item type is found.
                break;
            }

            _eventAggregator.GetEvent<ScreenProcessItemTypeReadyEvent>().Publish(new ScreenProcessItemTypeReadyEventParams
            {
                ProcessedScreen = currentScreenTooltipGui.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !string.IsNullOrWhiteSpace(_currentTooltip.ItemType);
        }

        private ItemTypeDescriptor FindItemType(Image<Gray, byte> currentTooltip, string currentItemType)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            // Template-based Image Matching

            // Initialization
            ItemTypeDescriptor itemType = new ItemTypeDescriptor { Name = currentItemType };
            Mat result = new Mat();
            Image<Gray, byte> currentItemTypeImage = new Image<Gray, byte>(0, 0);

            try
            {
                // Initialization
                lock (_lockCloneImage)
                {
                    currentItemTypeImage = _settingsManager.Settings.LiteMode ? _imageListItemTypesLite[currentItemType] : _imageListItemTypes[currentItemType];
                }

                currentItemTypeImage = currentItemTypeImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
                CvInvoke.MatchTemplate(currentTooltip, currentItemTypeImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);

                double minVal = 0.0;
                double maxVal = 0.0;
                Point minLoc = new Point();
                Point maxLoc = new Point();

                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);
                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemType}) Similarity: {String.Format("{0:0.0000000000}",minVal)}");
                if (minVal < _settingsManager.Settings.ThresholdSimilarityType)
                {
                    itemType.Similarity = minVal;
                    itemType.Location = new Rectangle(minLoc, currentItemTypeImage.Size);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return itemType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item affixes are found.</returns>
        private bool FindItemAffixLocations()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Convert the image to grayscale
            Image<Gray, byte> currentScreenTooltipFilter = new Image<Gray, byte>(_currentScreenTooltip.Width, _currentScreenTooltip.Height, new Gray(0));
            currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>();
            currentScreenTooltipFilter = currentScreenTooltipFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

            //// Clone image for GUI
            //var currentScreenTooltipGui = _currentScreenTooltip.Clone();

            // Create image for GUI
            var currentScreenTooltipGui = new Image<Bgr, byte>(currentScreenTooltipFilter.Width, currentScreenTooltipFilter.Height, new Bgr());
            currentScreenTooltipGui = currentScreenTooltipFilter.Convert<Bgr, byte>();

            ConcurrentBag<List<ItemAffixLocationDescriptor>> itemAffixLocationBag = new ConcurrentBag<List<ItemAffixLocationDescriptor>>();
            Parallel.ForEach(_imageListItemAffixLocations.Keys, itemAffixLocation =>
            {
                try
                {
                    itemAffixLocationBag.Add(FindItemAffixLocation(currentScreenTooltipFilter, Path.GetFileNameWithoutExtension(itemAffixLocation)));
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"{MethodBase.GetCurrentMethod()?.Name}");
                }
            });

            // Combine results
            var itemAffixLocations = new List<ItemAffixLocationDescriptor>();
            foreach ( var affixLocation in itemAffixLocationBag )
            {
                itemAffixLocations.AddRange(affixLocation);
            }

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

                CvInvoke.Polylines(currentScreenTooltipGui, points, true, new MCvScalar(0, 0, 255), 5);
            }

            _eventAggregator.GetEvent<ScreenProcessItemAffixLocationsReadyEvent>().Publish(new ScreenProcessItemAffixLocationsReadyEventParams
            {
                ProcessedScreen = currentScreenTooltipGui.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return itemAffixLocations.Any();
        }

        private List<ItemAffixLocationDescriptor> FindItemAffixLocation(Image<Gray, byte> currentTooltip, string currentItemAffixLocation)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            // Template-based Image Matching

            // Initialization
            List<ItemAffixLocationDescriptor> itemAffixLocations = new List<ItemAffixLocationDescriptor>();
            Mat result = new Mat();
            Image<Gray, byte> currentTooltipImage = new Image<Gray, byte>(0, 0);
            Image<Gray, byte> currentItemAffixLocationImage = new Image<Gray, byte>(0, 0);

            try
            {
                // Initialization
                lock (_lockCloneImage)
                {
                    currentTooltipImage = currentTooltip.Clone();
                    currentItemAffixLocationImage = _imageListItemAffixLocations[currentItemAffixLocation].Clone();
                }

                currentItemAffixLocationImage = currentItemAffixLocationImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

                int counter = 0;
                double minVal = 0.0;
                double maxVal = 0.0;
                Point minLoc = new Point();
                Point maxLoc = new Point();

                do
                {
                    counter++;

                    CvInvoke.MatchTemplate(currentTooltipImage, currentItemAffixLocationImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
                    CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                    //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAffixLocation}) Similarity: {String.Format("{0:0.0000000000}", minVal)}");

                    // Too many similarities. Need to add some constraints to filter out false positives.
                    if (minLoc.X < currentTooltipImage.Width / 7 && minVal < _settingsManager.Settings.ThresholdSimilarityAffixLocation)
                    //if (minLoc.X < 60 && minVal < similarityThreshold)
                    {
                        itemAffixLocations.Add(new ItemAffixLocationDescriptor
                        {
                            Similarity = minVal,
                            Location = new Rectangle(minLoc, currentItemAffixLocationImage.Size)
                        });
                    }

                    // Mark location so that it's only detected once.
                    var rectangle = new Rectangle(minLoc, currentItemAffixLocationImage.Size);
                    CvInvoke.Rectangle(currentTooltipImage, rectangle, new MCvScalar(255, 255, 255), -1);
                    //currentTooltipImage.Save($"Logging/currentTooltip{DateTime.Now.Ticks}_{currentItemAffixLocation}.png");

                } while (minVal < _settingsManager.Settings.ThresholdSimilarityAffixLocation && counter < 20);

                if (counter >= 20)
                {
                    _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                    {
                        Message = $"Too many affix locations found in tooltip. Aborted! Check images in debug view."
                    });
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return itemAffixLocations;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item affixes are found.</returns>
        private bool FindItemAffixes()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Convert the image to grayscale and apply threshold
            Image<Gray, byte> currentScreenTooltipFilter = new Image<Gray, byte>(_currentScreenTooltip.Width, _currentScreenTooltip.Height, new Gray(0));
            currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>();
            currentScreenTooltipFilter = currentScreenTooltipFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

            // Create image for GUI
            var currentScreenTooltipGui = new Image<Bgr, byte>(currentScreenTooltipFilter.Width, currentScreenTooltipFilter.Height, new Bgr());
            currentScreenTooltipGui = currentScreenTooltipFilter.Convert<Bgr, byte>();

            string affixPreset = _settingsManager.Settings.SelectedAffixName;
            var itemAffixes = _affixPresetManager.AffixPresets.FirstOrDefault(s => s.Name == affixPreset)?.ItemAffixes;
            var itemAffixesPerType = itemAffixes?.FindAll(itemAffix => _currentTooltip.ItemType.StartsWith($"{itemAffix.Type}_"));
            if (itemAffixesPerType != null) 
            {
                ConcurrentBag<List<ItemAffixDescriptor>> itemAffixBag = new ConcurrentBag<List<ItemAffixDescriptor>>();
                Parallel.ForEach(itemAffixesPerType, itemAffix =>
                {
                    try
                    {
                        itemAffixBag.Add(FindItemAffix(currentScreenTooltipFilter, Path.GetFileNameWithoutExtension(itemAffix.FileName)));
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, $"{MethodBase.GetCurrentMethod()?.Name}");
                    }
                });

                // Combine results
                var itemAffixResults = new List<ItemAffixDescriptor>();
                foreach (var itemAffix in itemAffixBag)
                {
                    itemAffixResults.AddRange(itemAffix);
                }

                // Sort results by similarity
                itemAffixResults.Sort((x, y) =>
                {
                    return x.Similarity < y.Similarity ? -1 : x.Similarity > y.Similarity ? 1 : 0;
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

                    CvInvoke.Polylines(currentScreenTooltipGui, points, true, new MCvScalar(0, 0, 255), 5);
                }
            }

            _eventAggregator.GetEvent<ScreenProcessItemAffixesReadyEvent>().Publish(new ScreenProcessItemAffixesReadyEventParams
            {
                ProcessedScreen = currentScreenTooltipGui.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return _currentTooltip.ItemAffixes.Any();
        }

        private List<ItemAffixDescriptor> FindItemAffix(Image<Gray, byte> currentTooltip, string currentItemAffix)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            // Template-based Image Matching

            // Initialization
            List<ItemAffixDescriptor> itemAffixes = new List<ItemAffixDescriptor>();
            Mat result = new Mat();
            Image<Gray, byte> currentTooltipImage = new Image<Gray, byte>(0, 0);
            Image<Gray, byte> currentItemAffixImage = new Image<Gray, byte>(0, 0);

            try
            {
                // Initialization
                lock (_lockCloneImage)
                {
                    currentTooltipImage = currentTooltip.Clone();
                    currentItemAffixImage = _imageListItemAffixes[currentItemAffix].Clone();
                }

                currentItemAffixImage = currentItemAffixImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

                int counter = 0;
                double minVal = 0.0;
                double maxVal = 0.0;
                Point minLoc = new Point();
                Point maxLoc = new Point();

                do
                {
                    counter++;

                    CvInvoke.MatchTemplate(currentTooltipImage, currentItemAffixImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
                    CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                    //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAffix}) Similarity: {String.Format("{0:0.0000000000}", minVal)}");

                    // Note: Ignore minVal == 0 results. Looks like they are random false positives. Requires more testing
                    // Unfortunately also valid matches, can't ignore the results.
                    //if (minVal < similarityThreshold && minVal != 0)
                    if (minVal < _settingsManager.Settings.ThresholdSimilarityAffix)
                    {
                        itemAffixes.Add(new ItemAffixDescriptor
                        {
                            Similarity = minVal,
                            Location = new Rectangle(minLoc, currentItemAffixImage.Size)
                        });
                    }

                    // Mark location so that it's only detected once.
                    var rectangle = new Rectangle(minLoc, currentItemAffixImage.Size);
                    CvInvoke.Rectangle(currentTooltipImage, rectangle, new MCvScalar(255, 255, 255), -1);
                    //currentTooltipImage.Save($"Logging/currentTooltip{DateTime.Now.Ticks}_{currentItemAffix}.png");

                } while (minVal < _settingsManager.Settings.ThresholdSimilarityAffix && counter < 10);

                if (counter >= 10)
                {
                    _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                    {
                        Message = $"Too many affixes found in tooltip. Aborted! Check images in debug view."
                    });
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return itemAffixes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item aspect is found.</returns>
        private bool FindItemAspectLocations()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Convert the image to grayscale
            Image<Gray, byte> currentScreenTooltipFilter = new Image<Gray, byte>(_currentScreenTooltip.Width, _currentScreenTooltip.Height, new Gray(0));
            currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>();
            //currentScreenRoiFilter = currentScreenRoiFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            
            // Clone image for GUI
            var currentScreenTooltipGui = _currentScreenTooltip.Clone();

            ConcurrentBag<ItemAspectLocationDescriptor> itemAspectLocationBag = new ConcurrentBag<ItemAspectLocationDescriptor>();
            Parallel.ForEach(_imageListItemAspectLocations.Keys, itemAspectLocation =>
            {
                try
                {
                    itemAspectLocationBag.Add(FindItemAspectLocation(currentScreenTooltipFilter, Path.GetFileNameWithoutExtension(itemAspectLocation)));
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"{MethodBase.GetCurrentMethod()?.Name}");
                }
            });

            // Sort results by accuracy
            var itemAspectLocations = itemAspectLocationBag.ToList();
            itemAspectLocations.Sort((x, y) =>
            {
                return x.Similarity < y.Similarity ? -1 : x.Similarity > y.Similarity ? 1 : 0;
            });

            foreach (var itemAspectLocation in itemAspectLocations)
            {
                if (itemAspectLocation.Location.IsEmpty) continue;

                _currentTooltip.ItemAspectLocation = itemAspectLocation.Location;

                Point[] points = new Point[]
                {
                    new Point(itemAspectLocation.Location.Left,itemAspectLocation.Location.Bottom),
                    new Point(itemAspectLocation.Location.Right,itemAspectLocation.Location.Bottom),
                    new Point(itemAspectLocation.Location.Right,itemAspectLocation.Location.Top),
                    new Point(itemAspectLocation.Location.Left,itemAspectLocation.Location.Top)
                };

                CvInvoke.Polylines(currentScreenTooltipGui, points, true, new MCvScalar(0, 0, 255), 5);

                // Skip foreach after the first valid aspect location is found.
                break;
            }

            _eventAggregator.GetEvent<ScreenProcessItemAspectLocationReadyEvent>().Publish(new ScreenProcessItemAspectLocationReadyEventParams
            {
                ProcessedScreen = currentScreenTooltipGui.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !_currentTooltip.ItemAspectLocation.IsEmpty;
        }

        private ItemAspectLocationDescriptor FindItemAspectLocation(Image<Gray, byte> currentTooltip, string currentItemAspectLocation)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            // Template-based Image Matching

            // Initialization
            ItemAspectLocationDescriptor itemAspectLocation = new ItemAspectLocationDescriptor();
            Mat result = new Mat();
            Image<Gray, byte> currentItemAspectImage = new Image<Gray, byte>(0, 0);

            try
            {
                // Initialization
                lock (_lockCloneImage) 
                {
                    currentItemAspectImage = _imageListItemAspectLocations[currentItemAspectLocation].Clone();
                }

                double minVal = 0.0;
                double maxVal = 0.0;
                Point minLoc = new Point();
                Point maxLoc = new Point();

                CvInvoke.MatchTemplate(currentTooltip, currentItemAspectImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAspectLocation}) Similarity: {String.Format("{0:0.0000000000}",minVal)}");

                if (minVal < _settingsManager.Settings.ThresholdSimilarityAspectLocation)
                {
                    itemAspectLocation.Similarity = minVal;
                    itemAspectLocation.Location = new Rectangle(minLoc, currentItemAspectImage.Size);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);

            }

            return itemAspectLocation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item aspect is found.</returns>
        private bool FindItemAspects()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Convert the image to grayscale and apply threshold
            Image<Gray, byte> currentScreenTooltipFilter = new Image<Gray, byte>(_currentScreenTooltip.Width, _currentScreenTooltip.Height, new Gray(0));
            currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>();
            currentScreenTooltipFilter = currentScreenTooltipFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

            // Create image for GUI
            var currentScreenTooltipGui = new Image<Bgr, byte>(currentScreenTooltipFilter.Width, currentScreenTooltipFilter.Height, new Bgr());
            currentScreenTooltipGui = currentScreenTooltipFilter.Convert<Bgr, byte>();

            string affixPreset = _settingsManager.Settings.SelectedAffixName;
            var itemAspects = _affixPresetManager.AffixPresets.FirstOrDefault(s => s.Name == affixPreset)?.ItemAspects;
            var itemAspectsPerType = itemAspects?.FindAll(itemAspect => _currentTooltip.ItemType.StartsWith($"{itemAspect.Type}_"));
            if (itemAspectsPerType != null)
            {
                ConcurrentBag<ItemAspectDescriptor> itemAspectBag = new ConcurrentBag<ItemAspectDescriptor>();
                Parallel.ForEach(itemAspectsPerType, itemAspect =>
                {
                    try
                    {
                        itemAspectBag.Add(FindItemAspect(currentScreenTooltipFilter, Path.GetFileNameWithoutExtension(itemAspect.FileName)));
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, $"{MethodBase.GetCurrentMethod()?.Name}");
                    }
                });

                // Sort results by similarity
                var itemAspectsResults = itemAspectBag.ToList();
                itemAspectsResults.Sort((x, y) =>
                {
                    return x.Similarity < y.Similarity ? -1 : x.Similarity > y.Similarity ? 1 : 0;
                });

                foreach (var itemAspect in itemAspectsResults)
                {
                    if (itemAspect.Location.IsEmpty) continue;

                    _currentTooltip.ItemAspect = itemAspect.Location;

                    Point[] points = new Point[]
                    {
                        new Point(itemAspect.Location.Left,itemAspect.Location.Bottom),
                        new Point(itemAspect.Location.Right,itemAspect.Location.Bottom),
                        new Point(itemAspect.Location.Right,itemAspect.Location.Top),
                        new Point(itemAspect.Location.Left,itemAspect.Location.Top)
                    };

                    CvInvoke.Polylines(currentScreenTooltipGui, points, true, new MCvScalar(0, 0, 255), 5);

                    // Skip foreach after the first valid aspect is found.
                    break;
                }
            }

            _eventAggregator.GetEvent<ScreenProcessItemAspectReadyEvent>().Publish(new ScreenProcessItemAspectReadyEventParams
            {
                ProcessedScreen = currentScreenTooltipGui.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !_currentTooltip.ItemAspect.IsEmpty;
        }

        private ItemAspectDescriptor FindItemAspect(Image<Gray, byte> currentTooltip, string currentItemAspect)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            // Template-based Image Matching

            // Initialization
            ItemAspectDescriptor itemAspect = new ItemAspectDescriptor();
            Mat result = new Mat();
            Image<Gray, byte> currentItemAspectImage = new Image<Gray, byte>(0, 0);

            try
            {
                // Initialization
                lock (_lockCloneImage)
                {
                    currentItemAspectImage = _imageListItemAspects[currentItemAspect].Clone();
                }

                currentItemAspectImage = currentItemAspectImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
                
                double minVal = 0.0;
                double maxVal = 0.0;
                Point minLoc = new Point();
                Point maxLoc = new Point();

                CvInvoke.MatchTemplate(currentTooltip, currentItemAspectImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAspect}) Similarity: {String.Format("{0:0.0000000000}", minVal)}");

                if (minVal < _settingsManager.Settings.ThresholdSimilarityAspect)
                {
                    itemAspect.Similarity = minVal;
                    itemAspect.Location = new Rectangle(minLoc, currentItemAspectImage.Size);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return itemAspect;
        }

        #endregion
    }
}
