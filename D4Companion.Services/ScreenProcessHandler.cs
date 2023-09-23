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
using System.Diagnostics.Metrics;
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
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixes = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAspectLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAspects = new Dictionary<string, Image<Gray, byte>>();
        private bool _isEnabled = false;
        private object _lockCloneImage = new object();
        private Task? _processTask = null;
        private bool _updateBrightnessThreshold = false;

        // Start of Constructors region

        #region Constructors

        public ScreenProcessHandler(IEventAggregator eventAggregator, ILogger<ScreenProcessHandler> logger, IAffixManager affixManager, 
            ISettingsManager settingsManager, ISystemPresetManager systemPresetManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
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
            LoadTemplateMatchingImageDirectory("Affixes\\Equipment", _imageListItemAffixes, null, true);
            LoadTemplateMatchingImageDirectory("Affixes\\Sigils", _imageListItemAffixes, null, true);
            LoadTemplateMatchingImageDirectory(string.Empty, _imageListItemAspectLocations, fileName => fileName.Contains("dot-aspects_"), true);
            LoadTemplateMatchingImageDirectory("Aspects\\Equipment", _imageListItemAspects, null, true);
            LoadTemplateMatchingImageDirectory("Aspects\\CagedHearts", _imageListItemAspects, null, true);
        }

        private void ProcessScreen(Bitmap currentScreen)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Reload images when the brightness threshold setting has been changed
                if (_updateBrightnessThreshold)
                {
                    LoadImageList();
                    _updateBrightnessThreshold = false;
                }

                // Clear previous tooltip
                _currentTooltip = new ItemTooltipDescriptor();

                if (currentScreen.Height < 100)
                {
                    _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Diablo IV window is probably minimized.");
                    return;
                }

                bool result = FindTooltips(currentScreen);
                
                // First search for both affix and aspect locations
                // Those are used to set the affix areas and limit the search area for the item types.
                if (result)
                {
                    FindItemAffixLocations();
                    FindItemAspectLocations();
                }

                // Set affix areas
                if (_currentTooltip.ItemAffixLocations.Any())
                {
                    FindItemAffixAreas();
                }

                if (result)
                {
                    result = FindItemTypes();
                }

                // Only search for affixes when the item tooltip contains them.
                if(_currentTooltip.ItemAffixLocations.Any())
                {
                    FindItemAffixes();
                }

                // Only search for aspects when the item tooltip contains one.
                if (!_currentTooltip.ItemAspectLocation.IsEmpty)
                {
                    FindItemAspects();
                }

                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Tooltip data ready:");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Item type: {_currentTooltip.ItemType}");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Item affixes: {_currentTooltip.ItemAffixes.Count}");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Item aspect: {!string.IsNullOrEmpty(_currentTooltip.ItemAspect.Id)}");

                _eventAggregator.GetEvent<TooltipDataReadyEvent>().Publish(new TooltipDataReadyEventParams
                {
                    Tooltip = _currentTooltip
                });

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
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
            var currentScreen = currentScreenSource.Copy(new Rectangle(scanPosX, 0, scanWidth, scanHeigth));

            // Handle window resize issues
            if (currentScreen.Width == 1) return false;

            // Convert the image to grayscale
            var currentScreenFilter = currentScreen.Convert<Gray, byte>();

            ConcurrentBag<ItemTooltipDescriptor> itemTooltipBag = new ConcurrentBag<ItemTooltipDescriptor>();
            Parallel.ForEach(_imageListItemTooltips.Keys, itemTooltip =>
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
                _currentTooltip.Offset = scanPosX;

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
                // Create ROI for current tooltip
                var location = _currentTooltip.Location;
                location.X += _currentTooltip.Offset;
                _currentScreenTooltip = currentScreenSource.Copy(location);
                _currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>().ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
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

                _currentTooltip.ItemType = itemType.Name;

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

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemType}) Similarity: {String.Format("{0:0.0000000000}",minVal)}");
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

        private void FindItemAffixAreas()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            //var watch = System.Diagnostics.Stopwatch.StartNew();

            int offsetAffixTop = 10;
            int offsetAffixWidth = 10;
            int offsetTooltipBottom = 10;

            List<Rectangle> areaStartPoints = new List<Rectangle>();
            areaStartPoints.AddRange(_currentTooltip.ItemAffixLocations);
            if (!_currentTooltip.ItemAspectLocation.IsEmpty) 
            {
                areaStartPoints.Add(_currentTooltip.ItemAspectLocation);
            }

            areaStartPoints.Sort((x, y) =>
            {
                return x.Top < y.Top ? -1 : x.Top > y.Top ? 1 : 0;
            });

            for (int i = 0; i < areaStartPoints.Count - 1; i++)
            {
                _currentTooltip.ItemAffixAreas.Add(new Rectangle(
                    areaStartPoints[i].X, areaStartPoints[i].Y - offsetAffixTop,
                    _settingsManager.Settings.TooltipWidth - areaStartPoints[i].X - offsetAffixWidth,
                    areaStartPoints[i + 1].Y - (areaStartPoints[i].Y- offsetAffixTop)));
            }

            if (_currentTooltip.ItemAspectLocation.IsEmpty)
            {
                _currentTooltip.ItemAffixAreas.Add(new Rectangle(
                    areaStartPoints[areaStartPoints.Count - 1].X, areaStartPoints[areaStartPoints.Count - 1].Y - offsetAffixTop,
                    _settingsManager.Settings.TooltipWidth - areaStartPoints[areaStartPoints.Count - 1].X - offsetAffixWidth,
                    _currentTooltip.Location.Height - areaStartPoints[areaStartPoints.Count - 1].Y - offsetTooltipBottom));
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

            //watch.Stop();
            //var elapsedMs = watch.ElapsedMilliseconds;
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

            string affixPreset = _settingsManager.Settings.SelectedAffixPreset;
            var affixPresetObj = _affixManager.AffixPresets.FirstOrDefault(s => s.Name == affixPreset);
            List<ItemAffix> itemAffixes = new List<ItemAffix>();
            itemAffixes.AddRange(affixPresetObj?.ItemAffixes ?? new List<ItemAffix>());
            itemAffixes.AddRange(affixPresetObj?.ItemSigils ?? new List<ItemAffix>());
            var itemAffixesPerType = itemAffixes?.FindAll(itemAffix => _currentTooltip.ItemType.StartsWith($"{itemAffix.Type}_"));

            if (itemAffixesPerType != null)
            {
                ConcurrentBag<ItemAffixDescriptor> itemAffixBag = new ConcurrentBag<ItemAffixDescriptor>();
                Parallel.For(0, areaImages.Count, index =>
                {
                    // Process all areas in parallel
                    Parallel.ForEach(itemAffixesPerType, itemAffix =>
                    {
                        // Process all affixes in parallel for each area
                        var mappedAffixImages = _systemPresetManager.GetMappedAffixImages(itemAffix.Id);
                        foreach (var mappedAffixImage in mappedAffixImages) 
                        {
                            // Process each mapped image (most of the time just one)
                            var itemAffixResult = FindItemAffix(areaImages[index], index, itemAffix, mappedAffixImage);
                            if (!itemAffixResult.Location.IsEmpty)
                            {
                                itemAffixBag.Add(itemAffixResult);
                            }
                        }
                    });
                });

                // Filter results. Don't add affixes if not all of the mapped images are found.
                foreach (var itemAffix in itemAffixBag)
                {
                    // Add results to image
                    var markedLocation = _currentTooltip.ItemAffixAreas[itemAffix.AreaIndex];
                    markedLocation.X += itemAffix.Location.X;
                    markedLocation.Y += itemAffix.Location.Y;
                    markedLocation.Width = itemAffix.Location.Width;
                    markedLocation.Height = itemAffix.Location.Height;

                    CvInvoke.Rectangle(currentScreenTooltip, markedLocation, new MCvScalar(0, 0, 255), 2);

                    // Skip if itemAffix already added
                    if (_currentTooltip.ItemAffixes.Any(affix => affix.Item1 == itemAffix.AreaIndex && affix.Item2.Id.Equals(itemAffix.ItemAffix.Id))) continue;

                    // Check if all images for affix are found
                    bool result = true;
                    var mappedAffixImages = _systemPresetManager.GetMappedAffixImages(itemAffix.ItemAffix.Id);
                    foreach (var mappedAffixImage in mappedAffixImages)
                    {
                        result = itemAffixBag.Any(affix => affix.ItemAffixMappedImage.Equals(mappedAffixImage) && affix.AreaIndex == itemAffix.AreaIndex);
                        if (!result) break;
                    }

                    // Validate result - When multiple affixes are found in the same area keep the one with the most images
                    if (result)
                    {
                        var addedAffix = _currentTooltip.ItemAffixes.FirstOrDefault(affix => affix.Item1 == itemAffix.AreaIndex);
                        if (addedAffix != null) 
                        {
                            if(_systemPresetManager.GetMappedAffixImages(addedAffix.Item2.Id).Count < mappedAffixImages.Count)
                            {
                                _currentTooltip.ItemAffixes.Remove(addedAffix);
                                _currentTooltip.ItemAffixes.Add(new Tuple<int, ItemAffix>(itemAffix.AreaIndex, itemAffix.ItemAffix));
                            }
                        }
                        else
                        {
                            _currentTooltip.ItemAffixes.Add(new Tuple<int, ItemAffix>(itemAffix.AreaIndex, itemAffix.ItemAffix));
                        }
                    }
                }
            }

            _eventAggregator.GetEvent<ScreenProcessItemAffixesReadyEvent>().Publish(new ScreenProcessItemAffixesReadyEventParams
            {
                ProcessedScreen = currentScreenTooltip.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return _currentTooltip.ItemAffixes.Any();
        }

        private ItemAffixDescriptor FindItemAffix(Image<Gray, byte> areaImageSource, int index, ItemAffix itemAffix, string mappedAffixImage)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            ItemAffixDescriptor itemAffixResult = new ItemAffixDescriptor();
            Image<Gray, byte> currentItemAffixImage;

            itemAffixResult.AreaIndex = index;
            itemAffixResult.ItemAffix = itemAffix;
            itemAffixResult.ItemAffixMappedImage = mappedAffixImage;

            try
            {
                lock (_lockCloneImage)
                {
                    currentItemAffixImage = _imageListItemAffixes[mappedAffixImage].Clone();
                }

                var (similarity, location) = FindMatchTemplate(areaImageSource, currentItemAffixImage);

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAffix}) Similarity: {String.Format("{0:0.0000000000}", minVal)}");

                if (similarity < _settingsManager.Settings.ThresholdSimilarityAffix)
                {
                    itemAffixResult.Similarity = similarity;
                    itemAffixResult.Location = new Rectangle(location, currentItemAffixImage.Size);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

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
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !_currentTooltip.ItemAspectLocation.IsEmpty;
        }

        private ItemAspectLocationDescriptor FindItemAspectLocation(Image<Gray, byte> currentTooltip, string currentItemAspectLocation)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            ItemAspectLocationDescriptor itemAspectLocation = new ItemAspectLocationDescriptor();
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

        /// <summary>
        /// Search the current item tooltip for the aspect.
        /// </summary>
        /// <returns>True when item aspect is found.</returns>
        private bool FindItemAspects()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreenTooltip = _currentScreenTooltipFilter.Convert<Bgr, byte>();

            string affixPreset = _settingsManager.Settings.SelectedAffixPreset;
            var itemAspects = _affixManager.AffixPresets.FirstOrDefault(s => s.Name == affixPreset)?.ItemAspects;
            var itemAspectsPerType = itemAspects?.FindAll(itemAspect => _currentTooltip.ItemType.StartsWith($"{itemAspect.Type}_"));
            if (itemAspectsPerType != null)
            {
                ConcurrentBag<ItemAspectDescriptor> itemAspectBag = new ConcurrentBag<ItemAspectDescriptor>();
                Parallel.ForEach(itemAspectsPerType, itemAspect =>
                {
                    // Process all aspects in parallel for each area (Aspects still use the complete tooltip as area for now).
                    var mappedAspectImages = _systemPresetManager.GetMappedAffixImages(itemAspect.Id);
                    foreach (var mappedAspectImage in mappedAspectImages)
                    {
                        // Process each mapped image (most of the time just one)
                        var itemAspectResult = FindItemAspect(_currentScreenTooltipFilter, itemAspect, mappedAspectImage);
                        if (!itemAspectResult.Location.IsEmpty)
                        {
                            itemAspectBag.Add(itemAspectResult);
                        }
                    }
                });

                // Sort results by similarity
                var itemAspectsResults = itemAspectBag.ToList();
                itemAspectsResults.Sort((x, y) =>
                {
                    return x.Similarity < y.Similarity ? -1 : x.Similarity > y.Similarity ? 1 : 0;
                });

                // Filter results. Don't add aspects if not all of the mapped images are found.
                foreach (var itemAspect in itemAspectsResults)
                {
                    // Add results to image
                    CvInvoke.Rectangle(currentScreenTooltip, itemAspect.Location, new MCvScalar(0, 0, 255), 2);

                    // Skip if itemAspect already added
                    if (_currentTooltip.ItemAspect.Id.Equals(itemAspect.ItemAspect.Id)) continue;

                    // Check if all images for aspect are found
                    bool result = true;
                    var mappedAspectImages = _systemPresetManager.GetMappedAffixImages(itemAspect.ItemAspect.Id);
                    foreach (var mappedAspectImage in mappedAspectImages)
                    {
                        result = itemAspectBag.Any(aspect => aspect.ItemAspectMappedImage.Equals(mappedAspectImage));
                        if (!result) break;
                    }

                    // Validate result - When multiple aspects are found keep the one with the most images
                    if (result)
                    {
                        if (string.IsNullOrEmpty(_currentTooltip.ItemAspect.Id) ||
                            _systemPresetManager.GetMappedAffixImages(_currentTooltip.ItemAspect.Id).Count < mappedAspectImages.Count)
                        {
                            _currentTooltip.ItemAspect = itemAspect.ItemAspect;
                        }
                    }
                }
            }

            _eventAggregator.GetEvent<ScreenProcessItemAspectReadyEvent>().Publish(new ScreenProcessItemAspectReadyEventParams
            {
                ProcessedScreen = currentScreenTooltip.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !string.IsNullOrEmpty(_currentTooltip.ItemAspect.Id);
        }

        private ItemAspectDescriptor FindItemAspect(Image<Gray, byte> currentTooltip, ItemAffix itemAspect, string mappedAspectImage)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            ItemAspectDescriptor itemAspectResult = new ItemAspectDescriptor();
            Image<Gray, byte> currentItemAspectImage;

            itemAspectResult.ItemAspect = itemAspect;
            itemAspectResult.ItemAspectMappedImage = mappedAspectImage;

            try
            {
                lock (_lockCloneImage)
                {
                    currentItemAspectImage = _imageListItemAspects[mappedAspectImage].Clone();
                }

                var (similarity, location) = FindMatchTemplate(currentTooltip, currentItemAspectImage);

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAspect}) Similarity: {String.Format("{0:0.0000000000}", minVal)}");

                if (similarity < _settingsManager.Settings.ThresholdSimilarityAspect)
                {
                    itemAspectResult.Similarity = similarity;
                    itemAspectResult.Location = new Rectangle(location, currentItemAspectImage.Size);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return itemAspectResult;
        }

        #endregion
    }
}
