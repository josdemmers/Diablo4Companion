using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace D4Companion.Services
{
    public class ScreenProcessHandler : IScreenProcessHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly IOcrHandler _ocrHandler;
        private readonly ISettingsManager _settingsManager;
        private readonly ISystemPresetManager _systemPresetManager;

        private int _mouseCoordsX;
        private int _mouseCoordsY;
        private Image<Bgr, byte> _currentScreenTooltip;
        private Image<Gray, byte> _currentScreenTooltipFilter;
        private ItemTooltipDescriptor _currentTooltip = new ItemTooltipDescriptor();
        Dictionary<string, Image<Gray, byte>> _imageListItemTooltips = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemTypes = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAspectLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemSocketLocations = new Dictionary<string, Image<Gray, byte>>();
        private bool _isEnabled = false;
        private object _lockCloneImage = new object();
        private object _lockOcrDebugInfo = new object();
        private string _previousItemType = string.Empty;
        private Task? _processTask = null;
        private bool _updateAvailableImages = false;
        private bool _updateBrightnessThreshold = false;

        // Start of Constructors region

        #region Constructors

        public ScreenProcessHandler(IEventAggregator eventAggregator, ILogger<ScreenProcessHandler> logger, IAffixManager affixManager,
            IOcrHandler ocrHandler, ISettingsManager settingsManager, ISystemPresetManager systemPresetManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AvailableImagesChangedEvent>().Subscribe(HandleAvailableImagesChangedEvent);
            _eventAggregator.GetEvent<BrightnessThresholdChangedEvent>().Subscribe(HandleBrightnessThresholdChangedEvent);
            _eventAggregator.GetEvent<ScreenCaptureReadyEvent>().Subscribe(HandleScreenCaptureReadyEvent);
            _eventAggregator.GetEvent<SystemPresetChangedEvent>().Subscribe(HandleSystemPresetChangedEvent);
            _eventAggregator.GetEvent<ToggleOverlayEvent>().Subscribe(HandleToggleOverlayEvent);
            _eventAggregator.GetEvent<ToggleOverlayFromGUIEvent>().Subscribe(HandleToggleOverlayFromGUIEvent);
            _eventAggregator.GetEvent<MouseUpdatedEvent>().Subscribe(HandleMouseUpdatedEvent);

            // Init logger
            _logger = logger;

            // Init services
            _affixManager = affixManager;
            _ocrHandler = ocrHandler;
            _settingsManager = settingsManager;
            _systemPresetManager = systemPresetManager;

            // Init image list.
            LoadImageList();
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

        private void HandleAvailableImagesChangedEvent()
        {
            _updateAvailableImages = true;
        }

        private void HandleBrightnessThresholdChangedEvent()
        {
            _updateBrightnessThreshold = true;
        }

        private void HandleScreenCaptureReadyEvent(ScreenCaptureReadyEventParams screenCaptureReadyEventParams)
        {
            if (!IsEnabled) return;
            if (_processTask != null && (_processTask.Status.Equals(TaskStatus.Running) || _processTask.Status.Equals(TaskStatus.WaitingForActivation))) return;
            if (screenCaptureReadyEventParams.CurrentScreen == null) return;

            _processTask?.Dispose();
            _processTask = Task.Run(() => ProcessScreen(screenCaptureReadyEventParams.CurrentScreen));
        }

        private void HandleSystemPresetChangedEvent()
        {
            LoadImageList();
        }

        private void HandleToggleOverlayEvent(ToggleOverlayEventParams toggleOverlayEventParams)
        {
            IsEnabled = toggleOverlayEventParams.IsEnabled;
        }

        private void HandleToggleOverlayFromGUIEvent(ToggleOverlayFromGUIEventParams toggleOverlayFromGUIEventParams)
        {
            IsEnabled = toggleOverlayFromGUIEventParams.IsEnabled;
        }

        private void HandleMouseUpdatedEvent(MouseUpdatedEventParams mouseUpdatedEventParams)
        {
            _mouseCoordsX = mouseUpdatedEventParams.CoordsMouseX;
            _mouseCoordsY = mouseUpdatedEventParams.CoordsMouseY;
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void LoadImageList()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            _imageListItemTooltips.Clear();
            _imageListItemTypes.Clear();
            _imageListItemAffixLocations.Clear();
            _imageListItemAspectLocations.Clear();
            _imageListItemSocketLocations.Clear();

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

            // Local function for loading template matching images
            void LoadTemplateMatchingImageDirectory(string folder, Dictionary<string, Image<Gray, byte>> imageDictionary, Func<string, bool>? fileNameFilter, bool applyBinaryThreshold)
            {
                directory = string.IsNullOrWhiteSpace(folder) ? $"Images\\{systemPreset}\\" : $"Images\\{systemPreset}\\{folder}\\";
                if (Directory.Exists(directory))
                {
                    var fileEntries = Directory.EnumerateFiles(directory).Where(tooltip => tooltip.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                    if (fileNameFilter != null)
                    {
                        fileEntries = fileEntries.Where(fileName => fileNameFilter(fileName));
                    }

                    foreach (string fileName in fileEntries)
                    {
                        var image = new Image<Gray, byte>(fileName);
                        if (applyBinaryThreshold) 
                        {
                            image = image.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
                        }

                        imageDictionary.TryAdd(Path.GetFileName(fileName).ToLower(), image);
                    }
                }
            }

            LoadTemplateMatchingImageDirectory("Tooltips", _imageListItemTooltips, null, false);
            LoadTemplateMatchingImageDirectory("Types", _imageListItemTypes, null, true);
            LoadTemplateMatchingImageDirectory(string.Empty, _imageListItemAffixLocations, fileName => fileName.Contains("dot-affixes_"), true);
            LoadTemplateMatchingImageDirectory(string.Empty, _imageListItemAspectLocations, fileName => fileName.Contains("dot-aspects_"), true);
            LoadTemplateMatchingImageDirectory(string.Empty, _imageListItemSocketLocations, fileName => fileName.Contains("dot-socket_"), true);
        }

        private void ProcessScreen(Bitmap currentScreen)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Reload images when new images have been added or the brightness threshold setting has been changed
                if (_updateAvailableImages || _updateBrightnessThreshold)
                {
                    LoadImageList();
                    _updateAvailableImages = false;
                    _updateBrightnessThreshold = false;
                }

                // Clear previous tooltip
                _previousItemType = _currentTooltip.ItemType;
                _currentTooltip = new ItemTooltipDescriptor();

                if (currentScreen.Height < 100)
                {
                    _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Diablo IV window is probably minimized.");
                    return;
                }

                bool result = FindTooltips(currentScreen);

                // First search for affix, aspect, and socket locations
                // Those are used to set the affix areas and limit the search area for the item types.
                if (result)
                {
                    FindItemAffixLocations();
                    FindItemAspectLocations();

                    // Remove invalid affixes before looking for the socket locations in case the compare tooltips option is turned on.
                    RemoveInvalidAffixLocations();

                    FindItemSocketLocations();

                }
                else
                {
                    // Reset item type info when there is no tooltip on your screen
                    _previousItemType = string.Empty;
                }

                // Set affix areas
                if (_currentTooltip.ItemAffixLocations.Any())
                {
                    FindItemAffixAreas();
                }

                // Set aspect areas
                if (!_currentTooltip.ItemAspectLocation.IsEmpty)
                {
                    FindItemAspectAreas();
                }

                if (result)
                {
                    result = FindItemTypes();
                    if (!result)
                    {
                        _currentTooltip.ItemType = _previousItemType;
                        result = !string.IsNullOrWhiteSpace(_currentTooltip.ItemType);
                        if (!result)
                        {
                            _eventAggregator.GetEvent<WarningOccurredEvent>().Publish(new WarningOccurredEventParams
                            {
                                Message = $"Unknown item type."
                            });
                        }
                    }
                }

                Parallel.Invoke(
                    () =>
                    {
                        // Only search for affixes when the item tooltip contains them.
                        if (_currentTooltip.ItemAffixLocations.Any())
                        {
                            FindItemAffixes();
                        }
                    },
                    () =>
                    {
                        // Only search for aspects when the item tooltip contains one.
                        if (!_currentTooltip.ItemAspectLocation.IsEmpty && !_currentTooltip.IsUniqueItem)
                        {
                            FindItemAspects();
                        }
                        else
                        {
                            // Remove unique aspect
                            _currentTooltip.ItemAspectLocation = new Rectangle();
                        }
                    });

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;

                _currentTooltip.PerformanceResults["Total"] = (int)elapsedMs;

                _eventAggregator.GetEvent<TooltipDataReadyEvent>().Publish(new TooltipDataReadyEventParams
                {
                    Tooltip = _currentTooltip
                });

                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Tooltip data ready:");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Item type: {_currentTooltip.ItemType}");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Item affixes: {_currentTooltip.ItemAffixes.Count}");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Item aspect: {!string.IsNullOrEmpty(_currentTooltip.ItemAspect.Id)}");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Total Elapsed time: {elapsedMs}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private static (double Similarity, Point Location) FindMatchTemplate(Image<Gray, byte> image, Image<Gray, byte> template)
        {
            var minVal = 0.0;
            var maxVal = 0.0;
            var minLoc = new Point();
            var maxLoc = new Point();
            var result = new Mat();

            CvInvoke.MatchTemplate(image, template, result, TemplateMatchingType.SqdiffNormed);
            CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

            return (minVal, minLoc);
        }

        private static (double Similarity, Point Location) FindMatchTemplateMasked(Image<Gray, byte> image, Image<Gray, byte> template, Image<Gray, byte> templateMasked)
        {
            var minVal = 0.0;
            var maxVal = 0.0;
            var minLoc = new Point();
            var maxLoc = new Point();
            var result = new Mat();

            CvInvoke.MatchTemplate(image, template, result, TemplateMatchingType.SqdiffNormed, templateMasked);
            CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

            return (minVal, minLoc);
        }

        /// <summary>
        /// Searches the current screen for an item tooltip.
        /// </summary>
        /// <returns>True when item tooltip is found.</returns>
        private bool FindTooltips(Bitmap currentScreenBitmap)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Create ROI for current tooltip
            int scanPosX = 0;
            int scanWidth = (int)(_settingsManager.Settings.TooltipWidth * 2.5);
            int scanHeigth = currentScreenBitmap.Height;
            scanPosX = Math.Max(0, _mouseCoordsX - (scanWidth / 2));
            scanPosX = scanPosX + scanWidth >= currentScreenBitmap.Width ? currentScreenBitmap.Width - scanWidth : scanPosX;
            var currentScreenSource = currentScreenBitmap.ToImage<Bgr, byte>();
            var currentScreen = _settingsManager.Settings.ControllerMode ? currentScreenSource.Clone() : currentScreenSource.Copy(new Rectangle(scanPosX, 0, scanWidth, scanHeigth));

            // Handle window resize issues
            if (currentScreen.Width == 1) return false;

            // Convert the image to grayscale
            var currentScreenFilter = currentScreen.Convert<Gray, byte>();

            // Filter Tooltip images
            var tooltipImagesFiltered = _settingsManager.Settings.ControllerMode ?
                _imageListItemTooltips.Keys.ToList().FindAll(t => t.StartsWith("tooltip_gc_", StringComparison.OrdinalIgnoreCase) && _systemPresetManager.IsControllerActive(t)) :
                _imageListItemTooltips.Keys.ToList().FindAll(t => t.StartsWith("tooltip_kb_", StringComparison.OrdinalIgnoreCase));

            ConcurrentBag<ItemTooltipDescriptor> itemTooltipBag = new ConcurrentBag<ItemTooltipDescriptor>();
            Parallel.ForEach(tooltipImagesFiltered, itemTooltip =>
            {
                itemTooltipBag.Add(FindTooltip(currentScreenFilter, itemTooltip));
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
                _currentTooltip.Offset = _settingsManager.Settings.ControllerMode ? 0 : scanPosX;

                CvInvoke.Rectangle(currentScreen, itemTooltip.Location, new MCvScalar(0, 0, 255), 2);

                // Skip foreach after the first valid tooltip is found.
                break;
            }

            _eventAggregator.GetEvent<ScreenProcessItemTooltipReadyEvent>().Publish(new ScreenProcessItemTooltipReadyEventParams
            {
                ProcessedScreen = currentScreen.ToBitmap()
            });

            var result = !_currentTooltip.Location.IsEmpty;
            if (result)
            {
                // Check if tooltip is out of bounds
                var location = _currentTooltip.Location;
                location.Width = currentScreenSource.Width > location.Width + location.X + _currentTooltip.Offset ? location.Width : currentScreenSource.Width - (location.X + _currentTooltip.Offset);
                _currentTooltip.Location = location;

                // Create ROI for current tooltip
                location.X += _currentTooltip.Offset;

                _currentScreenTooltip = currentScreenSource.Copy(location);
                _currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>().ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["Tooltip"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return result;
        }

        private ItemTooltipDescriptor FindTooltip(Image<Gray, byte> currentScreen, string currentItemTooltip)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            ItemTooltipDescriptor tooltip = new ItemTooltipDescriptor();
            Image<Gray, byte> currentItemTooltipImage;

            try
            {
                lock (_lockCloneImage)
                {
                    currentItemTooltipImage = _imageListItemTooltips[currentItemTooltip].Clone();
                }

                if (currentScreen.Width < currentItemTooltipImage.Width) return tooltip;

                var (similarity, location) = FindMatchTemplate(currentScreen, currentItemTooltipImage);

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemTooltip}) Similarity: {String.Format("{0:0.0000000000}", minVal)} @ {minLoc.X},{minLoc.Y}");

                if (similarity < _settingsManager.Settings.ThresholdSimilarityTooltip)
                {
                    tooltip.Similarity = similarity;
                    tooltip.Location = new Rectangle(new Point(location.X, 0), new Size(_settingsManager.Settings.TooltipWidth, location.Y));
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return tooltip;
        }

        /// <summary>
        /// Searches the current tooltip for the item type
        /// </summary>
        /// <returns>True when item type is found.</returns>
        private bool FindItemTypes()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreenTooltipFilter = _currentTooltip.ItemAffixAreas.Count > 0 ?
                _currentScreenTooltipFilter.Copy(new Rectangle(0, 0, _currentScreenTooltip.Width, _currentTooltip.ItemAffixAreas[0].Y)) :
                _currentScreenTooltipFilter;
            var currentScreenTooltip = currentScreenTooltipFilter.Convert<Bgr, byte>();

            ConcurrentBag<ItemTypeDescriptor> itemTypeBag = new ConcurrentBag<ItemTypeDescriptor>();
            Parallel.ForEach(_imageListItemTypes.Keys, itemType =>
            {
                var itemTypeResult = FindItemType(currentScreenTooltipFilter, itemType);
                if (!itemTypeResult.Location.IsEmpty) 
                { 
                    itemTypeBag.Add(itemTypeResult);
                }
            });

            // Sort results by similarity
            var itemTypes = itemTypeBag.ToList();
            itemTypes.Sort((x, y) =>
            {
                return x.Similarity < y.Similarity ? -1 : x.Similarity > y.Similarity ? 1 : 0;
            });

            // Remove type weapon_all when ranged, offhand_focus, offhand_shield, or offhand_totem are found.
            if (itemTypes.Any(type => type.Name.Contains(Constants.ItemTypeConstants.Offhand) || type.Name.Contains(Constants.ItemTypeConstants.Ranged)))
            {
                itemTypes.RemoveAll(type => type.Name.Contains(Constants.ItemTypeConstants.Weapon));
            }

            foreach ( var itemType in itemTypes) 
            {
                if (itemType.Location.IsEmpty) continue;

                _currentTooltip.ItemType = itemType.Name.Split("_")[0];

                CvInvoke.Rectangle(currentScreenTooltip, itemType.Location, new MCvScalar(0, 0, 255), 2);

                // Skip foreach after the first valid item type is found.
                break;
            }

            _eventAggregator.GetEvent<ScreenProcessItemTypeReadyEvent>().Publish(new ScreenProcessItemTypeReadyEventParams
            {
                ProcessedScreen = currentScreenTooltip.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !string.IsNullOrWhiteSpace(_currentTooltip.ItemType);
        }

        private ItemTypeDescriptor FindItemType(Image<Gray, byte> currentTooltip, string currentItemType)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            ItemTypeDescriptor itemType = new ItemTypeDescriptor { Name = currentItemType };
            Image<Gray, byte> currentItemTypeImage;

            try
            {
                lock (_lockCloneImage)
                {
                    currentItemTypeImage = _imageListItemTypes[currentItemType];
                }

                var (similarity, location) = FindMatchTemplate(currentTooltip, currentItemTypeImage);

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemType}) Similarity: {String.Format("{0:0.0000000000}", similarity)}");
                //currentItemTypeImage.Save($"Logging/currentTooltip{DateTime.Now.Ticks}_{currentItemType}.png");

                if (similarity < _settingsManager.Settings.ThresholdSimilarityType)
                {
                    itemType.Similarity = similarity;
                    itemType.Location = new Rectangle(location, currentItemTypeImage.Size);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return itemType;
        }

        /// <summary>
        /// Searches the current tooltip for the affix locations.
        /// </summary>
        /// <returns>True when item affixes are found.</returns>
        private bool FindItemAffixLocations()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreenTooltipFilter = _currentScreenTooltipFilter.Copy(new Rectangle(0, 0, _currentScreenTooltip.Width / 7, _currentScreenTooltip.Height));
            var currentScreenTooltip = currentScreenTooltipFilter.Convert<Bgr, byte>();

            ConcurrentBag<ItemAffixLocationDescriptor> itemAffixLocationBag = new ConcurrentBag<ItemAffixLocationDescriptor>();
            Parallel.ForEach(_imageListItemAffixLocations.Keys, itemAffixLocation =>
            {
                var itemAffixLocations = FindItemAffixLocation(currentScreenTooltipFilter, itemAffixLocation);
                itemAffixLocations.ForEach(itemAffixLocationBag.Add);
            });

            // Sort results by Y-Pos
            var itemAffixLocations = itemAffixLocationBag.ToList();
            itemAffixLocations.Sort((x, y) =>
            {
                return x.Location.Top < y.Location.Top ? -1 : x.Location.Top > y.Location.Top ? 1 : 0;
            });

            foreach (var itemAffixLocation in itemAffixLocations)
            {
                _currentTooltip.ItemAffixLocations.Add(itemAffixLocation.Location);

                CvInvoke.Rectangle(currentScreenTooltip, itemAffixLocation.Location, new MCvScalar(0, 0, 255), 2);
            }

            _eventAggregator.GetEvent<ScreenProcessItemAffixLocationsReadyEvent>().Publish(new ScreenProcessItemAffixLocationsReadyEventParams
            {
                ProcessedScreen = currentScreenTooltip.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["AffixLocations"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return _currentTooltip.ItemAffixLocations.Any();
        }

        private List<ItemAffixLocationDescriptor> FindItemAffixLocation(Image<Gray, byte> currentTooltipSource, string currentItemAffixLocation)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            List<ItemAffixLocationDescriptor> itemAffixLocations = new List<ItemAffixLocationDescriptor>();
            Image<Gray, byte> currentTooltip;
            Image<Gray, byte> currentItemAffixLocationImage;
            int counter = 0;
            double similarity = 0.0;
            Point location = Point.Empty;

            try
            {
                lock (_lockCloneImage)
                {
                    currentTooltip = currentTooltipSource.Clone();
                    currentItemAffixLocationImage = _imageListItemAffixLocations[currentItemAffixLocation].Clone();
                }

                do
                {
                    counter++;

                    (similarity, location) = FindMatchTemplate(currentTooltip, currentItemAffixLocationImage);

                    //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAffixLocation}) Similarity: {String.Format("{0:0.0000000000}", minVal)}");

                    if (similarity < _settingsManager.Settings.ThresholdSimilarityAffixLocation)
                    {
                        itemAffixLocations.Add(new ItemAffixLocationDescriptor
                        {
                            Similarity = similarity,
                            Location = new Rectangle(location, currentItemAffixLocationImage.Size)
                        });
                    }

                    // Mark location so that it's only detected once.
                    var rectangle = new Rectangle(location, currentItemAffixLocationImage.Size);
                    CvInvoke.Rectangle(currentTooltip, rectangle, new MCvScalar(255, 255, 255), -1);

                } while (similarity < _settingsManager.Settings.ThresholdSimilarityAffixLocation && counter < 20);

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

        private void RemoveInvalidAffixLocations()
        {
            if (_currentTooltip.ItemAspectLocation.IsEmpty) return;
            
            _currentTooltip.ItemAffixLocations.RemoveAll(loc => loc.Y >= _currentTooltip.ItemAspectLocation.Y);
        }

        private void FindItemAffixAreas()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // The width of the image to detect the affix-location is used to set the offset for the x-axis.
            int offsetAffixMarker = 0;
            if (_imageListItemAffixLocations.Keys.Any())
            {
                var affixMarkerImageName = _imageListItemAffixLocations.Keys.ToList()[0];
                offsetAffixMarker = (int)(_imageListItemAffixLocations[affixMarkerImageName].Width * 1.2);
            }

            List<Rectangle> areaStartPoints = new List<Rectangle>();
            areaStartPoints.AddRange(_currentTooltip.ItemAffixLocations);
            if (!_currentTooltip.ItemAspectLocation.IsEmpty) 
            {
                areaStartPoints.Add(_currentTooltip.ItemAspectLocation);
            }
            else if (_currentTooltip.ItemSocketLocations.Count > 0)
            {
                // An offset for the socket location is used because the ROI to look for sockets does not start at the top of the tooltip but after the latest affix/aspect location.
                int affixY = _currentTooltip.ItemAffixLocations.Count == 0 ? 0 : _currentTooltip.ItemAffixLocations[_currentTooltip.ItemAffixLocations.Count - 1].Y;
                int aspectY = _currentTooltip.ItemAspectLocation.IsEmpty ? 0 : _currentTooltip.ItemAspectLocation.Y;
                int offsetY = Math.Max(affixY, aspectY);

                areaStartPoints.Add(new Rectangle(
                    _currentTooltip.ItemSocketLocations[0].X,
                    _currentTooltip.ItemSocketLocations[0].Y + offsetY, 
                    _currentTooltip.ItemSocketLocations[0].Width, 
                    _currentTooltip.ItemSocketLocations[0].Height));
            }

            areaStartPoints.Sort((x, y) =>
            {
                return x.Top < y.Top ? -1 : x.Top > y.Top ? 1 : 0;
            });


            // Create ROIs for each affix and aspect based on the locations saved in the areaStartPoints
            for (int i = 0; i < areaStartPoints.Count - 1; i++)
            {
                _currentTooltip.ItemAffixAreas.Add(new Rectangle(
                    areaStartPoints[i].X + offsetAffixMarker, 
                    areaStartPoints[i].Y - _settingsManager.Settings.AffixAreaHeightOffsetTop,
                    _currentTooltip.Location.Width - areaStartPoints[i].X - offsetAffixMarker - _settingsManager.Settings.AffixAspectAreaWidthOffset,
                    (areaStartPoints[i + 1].Y - _settingsManager.Settings.AffixAreaHeightOffsetBottom) - (areaStartPoints[i].Y - _settingsManager.Settings.AffixAreaHeightOffsetTop)));
            }

            if (_currentTooltip.ItemAspectLocation.IsEmpty && _currentTooltip.ItemSocketLocations.Count == 0)
            {
                _currentTooltip.ItemAffixAreas.Add(new Rectangle(
                    areaStartPoints[areaStartPoints.Count - 1].X + offsetAffixMarker, 
                    areaStartPoints[areaStartPoints.Count - 1].Y - _settingsManager.Settings.AffixAreaHeightOffsetTop,
                    _currentTooltip.Location.Width - areaStartPoints[areaStartPoints.Count - 1].X - offsetAffixMarker - _settingsManager.Settings.AffixAspectAreaWidthOffset,
                    _currentTooltip.Location.Height - areaStartPoints[areaStartPoints.Count - 1].Y - _settingsManager.Settings.AffixAreaHeightOffsetTop));
            }

            var currentScreenTooltip = _currentScreenTooltipFilter.Convert<Bgr, byte>();
            foreach (var area in _currentTooltip.ItemAffixAreas) 
            {
                CvInvoke.Rectangle(currentScreenTooltip, area, new MCvScalar(0, 0, 255), 2);
            }

            _eventAggregator.GetEvent<ScreenProcessItemAffixAreasReadyEvent>().Publish(new ScreenProcessItemAffixAreasReadyEventParams
            {
                ProcessedScreen = currentScreenTooltip.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["AffixAreas"] = (int)elapsedMs;
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");
        }

        /// <summary>
        /// Search the current item tooltip for affixes.
        /// </summary>
        /// <returns>True when item affixes are found.</returns>
        private bool FindItemAffixes()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Create image for each area
            var currentScreenTooltip = _currentScreenTooltipFilter.Convert<Bgr, byte>();
            var areaImages = new List<Image<Gray, byte>>();
            foreach (var area in _currentTooltip.ItemAffixAreas)
            {
                areaImages.Add(_currentScreenTooltipFilter.Copy(area));
            }

            ConcurrentBag<ItemAffixDescriptor> itemAffixBag = new ConcurrentBag<ItemAffixDescriptor>();
            Parallel.For(0, areaImages.Count, index =>
            {
                // Process all areas in parallel
                var itemAffixResult = FindItemAffix(areaImages[index], index, _currentTooltip.ItemType);
                itemAffixBag.Add(itemAffixResult);
            });

            foreach (var itemAffix in itemAffixBag)
            {
                _currentTooltip.ItemAffixes.Add(new Tuple<int, ItemAffix>(itemAffix.AreaIndex, itemAffix.ItemAffix));
            }

            _eventAggregator.GetEvent<ScreenProcessItemAffixesReadyEvent>().Publish(new ScreenProcessItemAffixesReadyEventParams
            {
                ProcessedScreen = currentScreenTooltip.ToBitmap()
            });

            // OCR results
            _currentTooltip.OcrResultAffixes.Sort((x, y) =>
            {
                return x.AreaIndex < y.AreaIndex ? -1 : x.AreaIndex > y.AreaIndex ? 1 : 0;
            });
            _eventAggregator.GetEvent<ScreenProcessItemAffixesOcrReadyEvent>().Publish(new ScreenProcessItemAffixesOcrReadyEventParams
            {
                OcrResults = _currentTooltip.OcrResultAffixes
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["Affixes"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return _currentTooltip.ItemAffixes.Any();
        }

        private ItemAffixDescriptor FindItemAffix(Image<Gray, byte> areaImageSource, int index, string itemType)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            ItemAffixDescriptor itemAffixResult = new ItemAffixDescriptor();
            OcrResultDescriptor ocrResultDescriptor = new OcrResultDescriptor();
            itemAffixResult.AreaIndex = index;
            ocrResultDescriptor.AreaIndex = index;

            string rawText = _ocrHandler.ConvertToText(areaImageSource.ToBitmap());

            OcrResult ocrResult = itemType.Equals(Constants.ItemTypeConstants.Sigil, StringComparison.OrdinalIgnoreCase) ?
                _ocrHandler.ConvertToSigil(rawText) :
                _ocrHandler.ConvertToAffix(rawText);
            ocrResultDescriptor.OcrResult = ocrResult;

            ItemAffix itemAffix = itemType.Equals(Constants.ItemTypeConstants.Sigil, StringComparison.OrdinalIgnoreCase) ?
                _affixManager.GetSigil(ocrResult.AffixId, _currentTooltip.ItemType) :
                _affixManager.GetAffix(ocrResult.AffixId, _currentTooltip.ItemType);
            itemAffixResult.ItemAffix = itemAffix;

            // Add OCR debug info
            lock (_lockOcrDebugInfo)
            {
                _currentTooltip.OcrResultAffixes.Add(ocrResultDescriptor);
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Area: {index} Elapsed time: {elapsedMs}");

            return itemAffixResult;
        }

        /// <summary>
        /// Search the current item tooltip for the aspect location.
        /// </summary>
        /// <returns>True when item aspect is found.</returns>
        private bool FindItemAspectLocations()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreenTooltipFilter = _currentScreenTooltipFilter.Copy(new Rectangle(0, 0, _currentScreenTooltip.Width / 7, _currentScreenTooltip.Height));
            var currentScreenTooltip = currentScreenTooltipFilter.Convert<Bgr, byte>();

            ConcurrentBag<ItemAspectLocationDescriptor> itemAspectLocationBag = new ConcurrentBag<ItemAspectLocationDescriptor>();
            Parallel.ForEach(_imageListItemAspectLocations.Keys, itemAspectLocation =>
            {
                var aspectLocation = FindItemAspectLocation(currentScreenTooltipFilter, itemAspectLocation);
                if (!aspectLocation.Location.IsEmpty)
                {
                    itemAspectLocationBag.Add(aspectLocation);
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
                _currentTooltip.IsUniqueItem = itemAspectLocation.Name.Contains("_unique", StringComparison.OrdinalIgnoreCase);
                CvInvoke.Rectangle(currentScreenTooltip, itemAspectLocation.Location, new MCvScalar(0, 0, 255), 2);

                // Skip foreach after the first valid aspect location is found.
                break;
            }

            _eventAggregator.GetEvent<ScreenProcessItemAspectLocationReadyEvent>().Publish(new ScreenProcessItemAspectLocationReadyEventParams
            {
                ProcessedScreen = currentScreenTooltip.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["AspectLocations"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !_currentTooltip.ItemAspectLocation.IsEmpty;
        }

        private ItemAspectLocationDescriptor FindItemAspectLocation(Image<Gray, byte> currentTooltip, string currentItemAspectLocation)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            ItemAspectLocationDescriptor itemAspectLocation = new ItemAspectLocationDescriptor { Name = currentItemAspectLocation };
            Image<Gray, byte> currentItemAspectImage;

            try
            {
                lock (_lockCloneImage) 
                {
                    currentItemAspectImage = _imageListItemAspectLocations[currentItemAspectLocation].Clone();
                }

                var (similarity, location) = FindMatchTemplate(currentTooltip, currentItemAspectImage);

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAspectLocation}) Similarity: {String.Format("{0:0.0000000000}",minVal)}");

                if (similarity < _settingsManager.Settings.ThresholdSimilarityAspectLocation)
                {
                    itemAspectLocation.Similarity = similarity;
                    itemAspectLocation.Location = new Rectangle(location, currentItemAspectImage.Size);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return itemAspectLocation;
        }

        private void FindItemAspectAreas()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // The width of the image to detect the aspect-location is used to set the offset for the x-axis.
            int offsetAffixMarker = 0;
            if (_imageListItemAspectLocations.Keys.Any())
            {
                var affixMarkerImageName = _imageListItemAspectLocations.Keys.ToList()[0];
                offsetAffixMarker = (int)(_imageListItemAspectLocations[affixMarkerImageName].Width * 1.2);
            }

            // Reduce height when there are sockets
            int aspectAreaButtomY = _currentTooltip.ItemSocketLocations.Count > 0 ? 
                _currentTooltip.ItemSocketLocations[0].Y + _currentTooltip.ItemAspectLocation.Y : 
                _currentTooltip.Location.Height;

            _currentTooltip.ItemAspectArea = new Rectangle(
                _currentTooltip.ItemAspectLocation.X + offsetAffixMarker, 
                _currentTooltip.ItemAspectLocation.Y - _settingsManager.Settings.AspectAreaHeightOffsetTop,
                _currentTooltip.Location.Width - _currentTooltip.ItemAspectLocation.X - offsetAffixMarker - _settingsManager.Settings.AffixAspectAreaWidthOffset,
                aspectAreaButtomY - (_currentTooltip.ItemAspectLocation.Y - _settingsManager.Settings.AspectAreaHeightOffsetTop));

            var currentScreenTooltip = _currentScreenTooltipFilter.Convert<Bgr, byte>();
            CvInvoke.Rectangle(currentScreenTooltip, _currentTooltip.ItemAspectArea, new MCvScalar(0, 0, 255), 2);

            _eventAggregator.GetEvent<ScreenProcessItemAspectAreaReadyEvent>().Publish(new ScreenProcessItemAspectAreaReadyEventParams
            {
                ProcessedScreen = currentScreenTooltip.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["AspectAreas"] = (int)elapsedMs;
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");
        }

        /// <summary>
        /// Search the current item tooltip for the aspect.
        /// </summary>
        /// <returns>True when item aspect is found.</returns>
        private bool FindItemAspects()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var area = _currentScreenTooltipFilter.Copy(_currentTooltip.ItemAspectArea);
            var currentScreenTooltip = area.Convert<Bgr, byte>();

            var itemAspectResult = FindItemAspect(area, _currentTooltip.ItemType);
            _currentTooltip.ItemAspect = itemAspectResult.ItemAspect;

            _eventAggregator.GetEvent<ScreenProcessItemAspectReadyEvent>().Publish(new ScreenProcessItemAspectReadyEventParams
            {
                ProcessedScreen = currentScreenTooltip.ToBitmap()
            });

            // OCR results
            _eventAggregator.GetEvent<ScreenProcessItemAspectOcrReadyEvent>().Publish(new ScreenProcessItemAspectOcrReadyEventParams
            {
                OcrResult = _currentTooltip.OcrResultAspect
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["Aspects"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !string.IsNullOrEmpty(_currentTooltip.ItemAspect.Id);
        }

        private ItemAspectDescriptor FindItemAspect(Image<Gray, byte> areaImageSource, string itemType)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            ItemAspectDescriptor itemAspectResult = new ItemAspectDescriptor();
            string rawText = _ocrHandler.ConvertToText(areaImageSource.ToBitmap());
            OcrResult ocrResult = _ocrHandler.ConvertToAspect(rawText);
            itemAspectResult.ItemAspect = _affixManager.GetAspect(ocrResult.AffixId, itemType);

            _currentTooltip.OcrResultAspect = ocrResult;

            return itemAspectResult;
        }

        /// <summary>
        /// Search the current item tooltip for the socket location.
        /// </summary>
        /// <returns>True when item aspect is found.</returns>
        private bool FindItemSocketLocations()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Reduce search area
            int affixY = _currentTooltip.ItemAffixLocations.Count == 0 ? 0 : _currentTooltip.ItemAffixLocations[_currentTooltip.ItemAffixLocations.Count - 1].Y;
            int aspectY = _currentTooltip.ItemAspectLocation.IsEmpty ? 0 : _currentTooltip.ItemAspectLocation.Y;
            int offsetY = Math.Max(affixY, aspectY);

            var currentScreenTooltipFilter = _currentScreenTooltipFilter.Copy(new Rectangle(0, offsetY, _currentScreenTooltip.Width / 5, _currentScreenTooltip.Height - offsetY));
            var currentScreenTooltip = currentScreenTooltipFilter.Convert<Bgr, byte>();

            ConcurrentBag<ItemSocketLocationDescriptor> itemSocketLocationBag = new ConcurrentBag<ItemSocketLocationDescriptor>();
            Parallel.ForEach(_imageListItemSocketLocations.Keys.Where(image => !image.Contains("mask")), itemSocketLocation =>
            {
                var itemSocketLocations = FindItemSocketLocation(currentScreenTooltipFilter, itemSocketLocation);
                itemSocketLocations.ForEach(itemSocketLocationBag.Add);
            });

            // Sort results by Y-Pos
            var itemSocketLocations = itemSocketLocationBag.ToList();
            itemSocketLocations.Sort((x, y) =>
            {
                return x.Location.Top < y.Location.Top ? -1 : x.Location.Top > y.Location.Top ? 1 : 0;
            });

            foreach (var itemSocketLocation in itemSocketLocations)
            {
                _currentTooltip.ItemSocketLocations.Add(itemSocketLocation.Location);

                CvInvoke.Rectangle(currentScreenTooltip, itemSocketLocation.Location, new MCvScalar(0, 0, 255), 2);
            }

            _eventAggregator.GetEvent<ScreenProcessItemSocketLocationsReadyEvent>().Publish(new ScreenProcessItemSocketLocationsReadyEventParams
            {
                ProcessedScreen = currentScreenTooltip.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["SocketLocations"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return _currentTooltip.ItemSocketLocations.Any();
        }

        private List<ItemSocketLocationDescriptor> FindItemSocketLocation(Image<Gray, byte> currentTooltipSource, string currentItemSocketLocation)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            List<ItemSocketLocationDescriptor> itemSocketLocations = new List<ItemSocketLocationDescriptor>();
            Image<Gray, byte> currentTooltip;
            Image<Gray, byte> currentItemSocketLocationImage;
            Image<Gray, byte> currentItemSocketLocationImageMasked;
            int counter = 0;
            double similarity = 0.0;
            Point location = Point.Empty;

            try
            {
                lock (_lockCloneImage)
                {
                    currentTooltip = currentTooltipSource.Clone();
                    currentItemSocketLocationImage = _imageListItemSocketLocations[currentItemSocketLocation].Clone();

                    var maskKey = $"{Path.GetFileNameWithoutExtension(currentItemSocketLocation)}_mask.png";
                    if (!_imageListItemSocketLocations.ContainsKey(maskKey))
                    {
                        return itemSocketLocations;
                    }

                    currentItemSocketLocationImageMasked = _imageListItemSocketLocations[maskKey].Clone();
                }

                do
                {
                    counter++;

                    (similarity, location) = FindMatchTemplateMasked(currentTooltip, currentItemSocketLocationImage, currentItemSocketLocationImageMasked);

                    //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemSocketLocation}) Similarity: {String.Format("{0:0.0000000000}", similarity)}");

                    if (similarity < _settingsManager.Settings.ThresholdSimilaritySocketLocation)
                    {
                        itemSocketLocations.Add(new ItemSocketLocationDescriptor
                        {
                            Similarity = similarity,
                            Location = new Rectangle(location, currentItemSocketLocationImage.Size)
                        });
                    }

                    // Mark location so that it's only detected once.
                    var rectangle = new Rectangle(location, currentItemSocketLocationImage.Size);
                    CvInvoke.Rectangle(currentTooltip, rectangle, new MCvScalar(255, 255, 255), -1);

                } while (similarity < _settingsManager.Settings.ThresholdSimilaritySocketLocation && counter < 2);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return itemSocketLocations;
        }

        #endregion
    }
}
