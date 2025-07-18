﻿using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Media;

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
        private readonly ITradeItemManager _tradeItemManager;

        private int _mouseCoordsX;
        private int _mouseCoordsY;
        private Image<Bgr, byte> _currentScreenTooltip;
        private Image<Gray, byte> _currentScreenTooltipFilter;
        private ItemTooltipDescriptor _currentTooltip = new ItemTooltipDescriptor();
        Dictionary<string, Image<Gray, byte>> _imageListItemTooltips = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAspectLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemSocketLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemSplitterLocations = new Dictionary<string, Image<Gray, byte>>();
        private bool _isEnabled = false;
        private object _lockCloneImage = new object();
        private object _lockOcrDebugInfo = new object();
        private string _previousItemType = string.Empty;
        private int _previousItemPower = 0;
        private Task? _processTask = null;
        private bool _updateAvailableImages = false;
        private bool _updateBrightnessThreshold = false;

        // Start of Constructors region

        #region Constructors

        public ScreenProcessHandler(IEventAggregator eventAggregator, ILogger<ScreenProcessHandler> logger, IAffixManager affixManager,
            IOcrHandler ocrHandler, ISettingsManager settingsManager, ISystemPresetManager systemPresetManager, ITradeItemManager tradeItemManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AffixLanguageChangedEvent>().Subscribe(HandleAffixLanguageChangedEvent);
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
            _tradeItemManager = tradeItemManager;

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

        private void HandleAffixLanguageChangedEvent()
        {
            _updateAvailableImages = true;
        }

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

            // Note: Do not move this inside ProcessScreen task. It delays the garbage collection.
            // Publish empty tooltip to clear overlay when currentScreen is empty.
            if (screenCaptureReadyEventParams.CurrentScreen == null)
            {
                _currentTooltip = new ItemTooltipDescriptor();
                _eventAggregator.GetEvent<TooltipDataReadyEvent>().Publish(new TooltipDataReadyEventParams
                {
                    Tooltip = _currentTooltip
                });

                return;
            }

            _processTask?.Dispose();
            _processTask = Task.Run(() =>
            {
                ProcessScreen(screenCaptureReadyEventParams.CurrentScreen);
                Thread.Sleep(_settingsManager.Settings.OverlayUpdateDelay);
            });            
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
            _imageListItemAffixLocations.Clear();
            _imageListItemAspectLocations.Clear();
            _imageListItemSocketLocations.Clear();
            _imageListItemSplitterLocations.Clear();

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
            void LoadTemplateMatchingImageDirectory(string folder, Dictionary<string, Image<Gray, byte>> imageDictionary, Func<string, bool>? fileNameFilter, Func<string, bool>? languageFilter, bool applyBinaryThreshold)
            {
                directory = string.IsNullOrWhiteSpace(folder) ? $"Images\\{systemPreset}\\" : $"Images\\{systemPreset}\\{folder}\\";
                if (Directory.Exists(directory))
                {
                    var fileEntries = Directory.EnumerateFiles(directory).Where(tooltip => tooltip.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                    if (fileNameFilter != null)
                    {
                        fileEntries = fileEntries.Where(fileName => fileNameFilter(fileName));
                    }

                    if (languageFilter != null)
                    {
                        int count = fileEntries.Where(fileName => languageFilter(fileName)).Count();
                        if (count > 0)
                        {
                            fileEntries = fileEntries.Where(fileName => languageFilter(fileName));
                        }
                        else
                        {
                            // No specific language image available use the general one.
                            fileEntries = fileEntries.Where(fileName =>
                                !fileName.Contains("deDE",StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("enUS", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("esES", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("esMX", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("frFR", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("itIT", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("jaJP", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("koKR", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("plPL", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("ptBR", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("ruRU", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("trTR", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("zhCN", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Contains("zhTW", StringComparison.OrdinalIgnoreCase));
                        }
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

            string currentLanguage = _settingsManager.Settings.SelectedAffixLanguage;
            LoadTemplateMatchingImageDirectory("Tooltips", _imageListItemTooltips, fileName => fileName.Contains("tooltip_gc_"), language => language.Contains($"_{currentLanguage}", StringComparison.OrdinalIgnoreCase), false);
            LoadTemplateMatchingImageDirectory("Tooltips", _imageListItemTooltips, fileName => fileName.Contains("tooltip_kb_"), language => language.Contains($"_{currentLanguage}", StringComparison.OrdinalIgnoreCase), false);
            LoadTemplateMatchingImageDirectory(string.Empty, _imageListItemAffixLocations, fileName => fileName.Contains("dot-affixes_"), null, true);
            LoadTemplateMatchingImageDirectory(string.Empty, _imageListItemAspectLocations, fileName => fileName.Contains("dot-aspects_"), null, true);
            LoadTemplateMatchingImageDirectory(string.Empty, _imageListItemSocketLocations, fileName => fileName.Contains("dot-socket_"), null, true);
            LoadTemplateMatchingImageDirectory(string.Empty, _imageListItemSplitterLocations, fileName => fileName.Contains("dot-splitter_"), null, true);

            // Verify image list
            VerifyImageList();
        }

        private void VerifyImageList()
        {
            try
            {
                string systemPreset = _settingsManager.Settings.SelectedSystemPreset;
                string directory = $"Images\\{systemPreset}\\";
                var files = Directory.GetFiles(directory);

                void SendMissingPresetImageMessage(string file)
                {
                    _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                    {
                        Message = $"System preset {systemPreset} image missing: {file}"
                    });
                }

                if (!files.Any(f => f.Contains("dot-affixes_greater", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-affixes_greater.png");
                if (!files.Any(f => f.Contains("dot-affixes_normal", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-affixes_normal.png");
                if (!files.Any(f => f.Contains("dot-affixes_reroll", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-affixes_reroll.png");
                if (!files.Any(f => f.Contains("dot-affixes_temper_", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-affixes_temper_*.png");
                if (!files.Any(f => f.Contains("dot-affixes_rune_", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-affixes_rune_*.png");
                if (!files.Any(f => f.Contains("dot-aspects_legendary", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-aspects_legendary.png");
                if (!files.Any(f => f.Contains("dot-aspects_unique", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-aspects_unique.png");
                if (!files.Any(f => f.Contains("dot-aspects_mythic", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-aspects_mythic.png");
                if (!files.Any(f => f.Contains("dot-socket_1", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-socket_1.png");
                if (!files.Any(f => f.Contains("dot-socket_1_mask", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-socket_1_mask.png");
                if (!files.Any(f => f.Contains("dot-socket_invocation", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-socket_invocation.png");
                if (!files.Any(f => f.Contains("dot-socket_invocation_mask", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-socket_invocation_mask.png");
                if (!files.Any(f => f.Contains("dot-socket_ritual", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-socket_ritual.png");
                if (!files.Any(f => f.Contains("dot-socket_ritual_mask", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-socket_ritual_mask.png");
                if (!files.Any(f => f.Contains("dot-splitter_1", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-splitter_1.png");
                if (!files.Any(f => f.Contains("dot-splitter_top_1", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("dot-splitter_top_1.png");

                directory = $"Images\\{systemPreset}\\Tooltips\\";
                files = Directory.GetFiles(directory);

                if (!files.Any(f => f.Contains("tooltip_kb_all", StringComparison.OrdinalIgnoreCase))) SendMissingPresetImageMessage("tooltip_kb_all.png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void ProcessScreen(Bitmap? currentScreen)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Reload images when:
                // - New images have been added
                // - The brightness threshold setting has been changed
                // - Affix language has been changed
                if (_updateAvailableImages || _updateBrightnessThreshold)
                {
                    LoadImageList();
                    _updateAvailableImages = false;
                    _updateBrightnessThreshold = false;
                }

                // Clear previous tooltip
                _previousItemPower = _currentTooltip.ItemPower;
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
                    FindItemSocketLocations();
                    FindItemSplitterLocations();

                    // Remove invalid socket locations based on affix locations.
                    RemoveInvalidSocketLocations();
                    // Remove invalid affix locations based on aspect and socket locations.
                    RemoveInvalidAffixLocations();
                }
                else
                {
                    // Reset item type info when there is no tooltip on your screen
                    _previousItemPower = 0;
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

                // Processes top of tooltip for itemtypes and power info
                if (result)
                {
                    if (_currentTooltip.ItemSplitterLocations.Any() && _currentTooltip.HasTooltipTopSplitter)
                    {
                        result = FindItemTypesPower();
                    }
                    else
                    {
                        // Restore last known values for item tooltips with scrollbar
                        _currentTooltip.ItemPower = _previousItemPower;
                        _currentTooltip.ItemType = _previousItemType;
                        result = !string.IsNullOrWhiteSpace(_currentTooltip.ItemType);
                    }

                    // Clear affix/aspect locations when itemtype is not found.
                    if (!result)
                    {
                        _currentTooltip.ItemAffixLocations.Clear();
                        _currentTooltip.ItemAspectLocation = new Rectangle();
                    }
                }

                // Skip disabled item types
                if (_currentTooltip.ItemType.Equals(ItemTypeConstants.Rune) && !_settingsManager.Settings.IsRuneDetectionEnabled)
                {
                    _currentTooltip.ItemAffixLocations.Clear();
                }

                // Skip ignored item types - Seasonal
                if (_currentTooltip.ItemType.Equals(ItemTypeConstants.OccultGem) ||
                    _currentTooltip.ItemType.Equals(ItemTypeConstants.WitcherSigil) ||
                    _currentTooltip.ItemType.Equals(ItemTypeConstants.DungeonEscalation) ||
                    _currentTooltip.ItemType.Equals(ItemTypeConstants.HoradricJewel))
                {
                    _currentTooltip.ItemAffixLocations.Clear();
                }

                // Update affix areas
                // - Requires item type information
                if (_currentTooltip.ItemAffixLocations.Any())
                {
                    UpdateItemAffixAreas();
                }

                Parallel.Invoke(
                    () =>
                    {
                        // Only search for affixes when the item tooltip contains them.
                        if (_currentTooltip.ItemAffixLocations.Any())
                        {
                            FindItemAffixes();
                            UpdateItemAffixAreasWithOcrResults();
                        }
                    },
                    () =>
                    {
                        // Aspect detection should be enabled,
                        // and only search for aspects when the item tooltip contains one.
                        if (_settingsManager.Settings.IsAspectDetectionEnabled &&
                        !_currentTooltip.ItemAspectLocation.IsEmpty && !_currentTooltip.IsUniqueItem)
                        {
                            FindItemAspects();
                        }
                        else if (_settingsManager.Settings.IsUniqueDetectionEnabled &&
                        !_currentTooltip.ItemAspectLocation.IsEmpty && _currentTooltip.IsUniqueItem)
                        {
                            FindItemUniqueAspects();
                        }
                        else
                        {
                            // Remove unique aspect
                            _currentTooltip.ItemAspectLocation = new Rectangle();
                        }
                    });

                // Find tradable item
                if (_settingsManager.Settings.IsTradeOverlayEnabled && _currentTooltip.ItemAffixes.Any())
                {
                    _currentTooltip.TradeItem = _tradeItemManager.FindTradeItem(_currentTooltip.ItemType, _currentTooltip.ItemAffixes, _currentTooltip.ItemAffixAreas);
                }

                // Multi build mode
                if (_settingsManager.Settings.IsMultiBuildModeEnabled)
                {
                    SetMultiBuildMode();
                }

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;

                _currentTooltip.PerformanceResults["Total"] = (int)elapsedMs;

                _eventAggregator.GetEvent<TooltipDataReadyEvent>().Publish(new TooltipDataReadyEventParams
                {
                    Tooltip = _currentTooltip
                });

                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Tooltip data ready:");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}:    Item type: {_currentTooltip.ItemType}");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}:    Item power: {_currentTooltip.ItemPower}");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}:    Item affixes: {_currentTooltip.ItemAffixes.Count}");
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}:    Item aspect: {!string.IsNullOrEmpty(_currentTooltip.ItemAspect.Id)}");
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

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            // Create ROI for current tooltip
            int scanPosX = 0;
            int scanWidth = (int)(_settingsManager.Settings.TooltipWidth * 2.5);
            int scanHeigth = (int)(currentScreenBitmap.Height * (_settingsManager.Settings.ScanHeight / 100.0));
            int scanPosY = currentScreenBitmap.Height - scanHeigth;
            scanPosX = Math.Max(0, _mouseCoordsX - (scanWidth / 2));
            scanPosX = scanPosX + scanWidth >= currentScreenBitmap.Width ? currentScreenBitmap.Width - scanWidth : scanPosX;
            var currentScreenSource = currentScreenBitmap.ToImage<Bgr, byte>();
            var currentScreen = _settingsManager.Settings.ControllerMode ? currentScreenSource.Clone() : currentScreenSource.Copy(new Rectangle(scanPosX, scanPosY, scanWidth, scanHeigth));

            // Handle window resize issues
            if (currentScreen.Width == 1) return false;

            // Convert the image to grayscale
            var currentScreenFilter = currentScreen.Convert<Gray, byte>();

            // Filter Tooltip images
            var tooltipImagesFiltered = _settingsManager.Settings.ControllerMode ?
                _imageListItemTooltips.Keys.ToList().FindAll(t => (t.StartsWith("tooltip_gc_", StringComparison.OrdinalIgnoreCase) || t.StartsWith("tooltip_kb_", StringComparison.OrdinalIgnoreCase)) && _systemPresetManager.IsControllerActive(t)) :
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
                _currentTooltip.OffsetX = _settingsManager.Settings.ControllerMode ? 0 : scanPosX;
                _currentTooltip.OffsetY = _settingsManager.Settings.ControllerMode ? 0 : scanPosY;

                if (IsDebugInfoEnabled)
                {
                    CvInvoke.Rectangle(currentScreen, itemTooltip.Location, new MCvScalar(0, 0, 255), 2);
                }

                // Skip foreach after the first valid tooltip is found.
                break;
            }

            if (IsDebugInfoEnabled)
            {
                _eventAggregator.GetEvent<ScreenProcessItemTooltipReadyEvent>().Publish(new ScreenProcessItemTooltipReadyEventParams
                {
                    ProcessedScreen = currentScreen.ToBitmap()
                });
            }

            var result = !_currentTooltip.Location.IsEmpty;
            if (result)
            {
                // Check if tooltip is out of bounds
                var location = _currentTooltip.Location;
                location.Width = currentScreenSource.Width > location.Width + location.X + _currentTooltip.OffsetX ? location.Width : currentScreenSource.Width - (location.X + _currentTooltip.OffsetX);
                location.Height = _currentTooltip.OffsetY + location.Height;
                _currentTooltip.Location = location;

                // Create ROI for current tooltip
                location.X += _currentTooltip.OffsetX;
                location.Y = 0;

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
        /// Processes top of tooltip for itemtypes and power info
        /// </summary>
        /// <returns></returns>
        private bool FindItemTypesPower()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            int startY = Math.Max(0, _currentTooltip.ItemSplitterLocations[0].Location.Y - _settingsManager.Settings.TooltipMaxHeight);
            int height = Math.Min(_currentTooltip.ItemSplitterLocations[0].Location.Y, _settingsManager.Settings.TooltipMaxHeight);
            var area = _currentTooltip.ItemSplitterLocations.Count > 0 ?
                _currentScreenTooltipFilter.Copy(new Rectangle(0, startY, _currentScreenTooltip.Width, height)) :
                _currentScreenTooltipFilter;

            FindItemTypePower(area);

            // OCR results
            _eventAggregator.GetEvent<ScreenProcessItemTypePowerOcrReadyEvent>().Publish(new ScreenProcessItemTypePowerOcrReadyEventParams
            {
                OcrResultPower = _currentTooltip.OcrResultPower,
                OcrResultItemType = _currentTooltip.OcrResultItemType
            });

            _currentTooltip.ItemPower = string.IsNullOrWhiteSpace(_currentTooltip.OcrResultPower.TextClean) ? 0 : int.Parse(_currentTooltip.OcrResultPower.TextClean);
            _currentTooltip.ItemType = string.IsNullOrWhiteSpace(_currentTooltip.OcrResultItemType.TypeId) ? _previousItemType : _currentTooltip.OcrResultItemType.TypeId;
            bool result = !string.IsNullOrWhiteSpace(_currentTooltip.ItemType);

            if (IsDebugInfoEnabled)
            {
                _eventAggregator.GetEvent<ScreenProcessItemTypeReadyEvent>().Publish(new ScreenProcessItemTypeReadyEventParams
                {
                    ProcessedScreen = area.ToBitmap()
                });
            }
            
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["ItemTypePower"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return result;
        }

        private void FindItemTypePower(Image<Gray, byte> areaImageSource)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            string rawText = _ocrHandler.ConvertToTextUpperTooltipSection(areaImageSource.ToBitmap());
            OcrResult ocrResultPower = _ocrHandler.ConvertToPower(rawText);
            OcrResultItemType ocrResultItemType = _ocrHandler.ConvertToItemType(rawText);

            _currentTooltip.OcrResultPower = ocrResultPower;
            _currentTooltip.OcrResultItemType = ocrResultItemType;
        }

        /// <summary>
        /// Searches the current tooltip for the affix locations.
        /// </summary>
        /// <returns>True when item affixes are found.</returns>
        private bool FindItemAffixLocations()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            var currentScreenTooltipFilter = _currentScreenTooltipFilter.Copy(new Rectangle(0, 0, _currentScreenTooltip.Width / 7, _currentScreenTooltip.Height));
            Image<Bgr, byte>? currentScreenTooltip = IsDebugInfoEnabled ? currentScreenTooltipFilter.Convert<Bgr, byte>() : null;

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
                _currentTooltip.ItemAffixLocations.Add(itemAffixLocation);

                if (IsDebugInfoEnabled)
                {
                    CvInvoke.Rectangle(currentScreenTooltip, itemAffixLocation.Location, new MCvScalar(0, 0, 255), 2);
                }
            }

            if (IsDebugInfoEnabled)
            {
                _eventAggregator.GetEvent<ScreenProcessItemAffixLocationsReadyEvent>().Publish(new ScreenProcessItemAffixLocationsReadyEventParams
                {
                    ProcessedScreen = currentScreenTooltip.ToBitmap()
                });
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["AffixLocations"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return _currentTooltip.ItemAffixLocations.Any();
        }

        private List<ItemAffixLocationDescriptor> FindItemAffixLocation(Image<Gray, byte> currentTooltipSource, string currentItemAffixLocation)
        {
            //_logger.LogDebug(string.Empty);
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

                    //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAffixLocation}) Similarity: {String.Format("{0:0.0000000000}", similarity)}");

                    if (similarity < _settingsManager.Settings.ThresholdSimilarityAffixLocation)
                    {
                        itemAffixLocations.Add(new ItemAffixLocationDescriptor
                        {
                            Similarity = similarity,
                            Location = new Rectangle(location, currentItemAffixLocationImage.Size),
                            Name = currentItemAffixLocation
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
            
            _currentTooltip.ItemAffixLocations.RemoveAll(loc => loc.Location.Y >= _currentTooltip.ItemAspectLocation.Y);

            if (_currentTooltip.ItemSocketLocations.Count == 0) return;

            // An offset for the socket location is used because the ROI to look for sockets does not start at the top of the tooltip but after the aspect location.
            int offsetY = _currentTooltip.ItemAspectLocation.IsEmpty ? 0 : _currentTooltip.ItemAspectLocation.Y;
            _currentTooltip.ItemAffixLocations.RemoveAll(loc => loc.Location.Y >= _currentTooltip.ItemSocketLocations[0].Y + offsetY);
        }

        private void RemoveInvalidSocketLocations()
        {
            // An offset for the socket location is used because the ROI to look for sockets does not start at the top of the tooltip but after the aspect location.
            int offsetY = _currentTooltip.ItemAspectLocation.IsEmpty ? 0 : _currentTooltip.ItemAspectLocation.Y;

            if (_currentTooltip.ItemAffixLocations.Count == 0) return;

            _currentTooltip.ItemSocketLocations.RemoveAll(loc => loc.Y + offsetY <= _currentTooltip.ItemAffixLocations[0].Location.Y);
        }

        private void FindItemAffixAreas()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            // The width of the image to detect the affix-location is used to set the offset for the x-axis.
            int offsetAffixMarker = 0;
            if (_imageListItemAffixLocations.Keys.Any())
            {
                var affixMarkerImageName = _imageListItemAffixLocations.Keys.ToList()[0];
                offsetAffixMarker = (int)(_imageListItemAffixLocations[affixMarkerImageName].Width * 1.2);
            }

            List<Rectangle> areaSplitPoints = new List<Rectangle>();
            // Affix locations
            areaSplitPoints.AddRange(_currentTooltip.ItemAffixLocations.Select(loc => loc.Location));
            // Aspect location
            if (!_currentTooltip.ItemAspectLocation.IsEmpty) areaSplitPoints.Add(_currentTooltip.ItemAspectLocation);
            // Socket location
            if (_currentTooltip.ItemSocketLocations.Count > 0)
            {
                // An offset for the socket location is used because the ROI to look for sockets does not start at the top of the tooltip but after the aspect location.
                int offsetY = _currentTooltip.ItemAspectLocation.IsEmpty ? 0 : _currentTooltip.ItemAspectLocation.Y;

                areaSplitPoints.Add(new Rectangle(
                    _currentTooltip.ItemSocketLocations[0].X,
                    _currentTooltip.ItemSocketLocations[0].Y + offsetY,
                    _currentTooltip.ItemSocketLocations[0].Width,
                    _currentTooltip.ItemSocketLocations[0].Height));
            }
            // Splitter locations
            areaSplitPoints.AddRange(_currentTooltip.ItemSplitterLocations.Select(s => s.Location));

            // Sort all point on their y-coordinates.
            areaSplitPoints.Sort((x, y) =>
            {
                return x.Top < y.Top ? -1 : x.Top > y.Top ? 1 : 0;
            });

            // Create affix areas
            foreach (var affixLocation in _currentTooltip.ItemAffixLocations)
            {
                var splitterLocation = areaSplitPoints.FirstOrDefault(loc => loc.Y > affixLocation.Location.Y);

                int yCoordsNextPoint = (splitterLocation.IsEmpty) ? _currentTooltip.Location.Height : splitterLocation.Y - _settingsManager.Settings.AffixAreaHeightOffsetBottom;

                _currentTooltip.ItemAffixAreas.Add(new ItemAffixAreaDescriptor
                {
                    Location = new Rectangle(
                        affixLocation.Location.X + offsetAffixMarker,
                        affixLocation.Location.Y - _settingsManager.Settings.AffixAreaHeightOffsetTop,
                        _currentTooltip.Location.Width - affixLocation.Location.X - offsetAffixMarker - _settingsManager.Settings.AffixAspectAreaWidthOffset,
                        yCoordsNextPoint - (affixLocation.Location.Y - _settingsManager.Settings.AffixAreaHeightOffsetTop)),
                    AffixType = affixLocation.Name.StartsWith("dot-affixes_normal") || affixLocation.Name.StartsWith("dot-affixes_reroll") ? Constants.AffixTypeConstants.Normal :
                    affixLocation.Name.StartsWith("dot-affixes_greater") ? Constants.AffixTypeConstants.Greater :
                    affixLocation.Name.StartsWith("dot-affixes_temper") ? Constants.AffixTypeConstants.Tempered :
                    affixLocation.Name.StartsWith("dot-affixes_rune") ? Constants.AffixTypeConstants.Rune : Constants.AffixTypeConstants.Normal
                });
            }

            if (IsDebugInfoEnabled)
            {
                var currentScreenTooltip = _currentScreenTooltipFilter.Convert<Bgr, byte>();
                foreach (var area in _currentTooltip.ItemAffixAreas)
                {
                    CvInvoke.Rectangle(currentScreenTooltip, area.Location, new MCvScalar(0, 0, 255), 2);
                }

                _eventAggregator.GetEvent<ScreenProcessItemAffixAreasReadyEvent>().Publish(new ScreenProcessItemAffixAreasReadyEventParams
                {
                    ProcessedScreen = currentScreenTooltip.ToBitmap()
                });
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["AffixAreas"] = (int)elapsedMs;
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");
        }

        /// <summary>
        /// Update affix areas with extra information.
        /// - Implicit affixes.
        /// </summary>
        private void UpdateItemAffixAreas()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            // Check if there are any areas with implicit affixes.
            // - Types with implicit affixes: Amulet, Boots, Offhand, Ranged, Ring, Weapon.
            // - Implicit area is between a top splitter and the first normal splitter.
            if (_currentTooltip.ItemType.Equals(ItemTypeConstants.Amulet) ||
                _currentTooltip.ItemType.Equals(ItemTypeConstants.Boots) ||
                _currentTooltip.ItemType.Equals(ItemTypeConstants.Offhand) ||
                _currentTooltip.ItemType.Equals(ItemTypeConstants.Ranged) ||
                _currentTooltip.ItemType.Equals(ItemTypeConstants.Ring) ||
                _currentTooltip.ItemType.Equals(ItemTypeConstants.Weapon))
            {
                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ItemType: {_currentTooltip.ItemType}");
                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: HasTooltipTopSplitter: {_currentTooltip.HasTooltipTopSplitter}");

                // Skip if top splitter icon is missing
                if (!_currentTooltip.HasTooltipTopSplitter) return;

                // List splitter variants
                var splittersTop = _currentTooltip.ItemSplitterLocations.FindAll(s => s.Name.Contains("dot-splitter_top"));
                var splitters = _currentTooltip.ItemSplitterLocations.FindAll(s => !s.Name.Contains("dot-splitter_top"));

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Count splittersTop: {splittersTop.Count}");
                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Count splitters: {splitters.Count}");

                // Skip if splitters are missing
                if (splittersTop.Count == 0) return;
                if (splitters.Count == 0) return;

                int implicitBeginY = splittersTop[0].Location.Y;
                int implicitEndY = splitters[0].Location.Y;

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: implicitBeginY: {implicitBeginY}");
                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: implicitEndY: {implicitEndY}");

                // Validate implicit area
                // - No greater affixes
                // - No tempered affixes
                foreach (var itemAffixArea in _currentTooltip.ItemAffixAreas)
                {
                    if (implicitBeginY <= itemAffixArea.Location.Y && itemAffixArea.Location.Y <= implicitEndY)
                    {
                        if (itemAffixArea.AffixType.Equals(AffixTypeConstants.Greater) ||
                            itemAffixArea.AffixType.Equals(AffixTypeConstants.Tempered))
                        {
                            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Invalid implicit area. Type {itemAffixArea.AffixType} found.");

                            // Skip implicit affixes for this item. Implicit area is invalid.
                            // Probably caused because one of the splitter icons was not detected.
                            return;
                        }
                    }
                }

                // Validate implicit area
                // - No aspects
                if (!_currentTooltip.ItemAspectLocation.IsEmpty)
                {
                    if (implicitBeginY <= _currentTooltip.ItemAspectLocation.Location.Y && _currentTooltip.ItemAspectLocation.Location.Y <= implicitEndY)
                    {
                        //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Invalid implicit area. Contains aspect.");

                        // Skip implicit affixes for this item. Implicit area is invalid.
                        // Probably caused because one of the splitter icons was not detected.
                        return;
                    }
                }

                // Set implicit affix
                foreach (var itemAffixArea in _currentTooltip.ItemAffixAreas)
                {
                    if (implicitBeginY <= itemAffixArea.Location.Y && itemAffixArea.Location.Y <= implicitEndY)
                    {
                        //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Implicit affix found.");

                        itemAffixArea.AffixType = AffixTypeConstants.Implicit;
                    }
                }
            }

            //_logger.LogDebug($"~{MethodBase.GetCurrentMethod()?.Name}");
        }

        private void UpdateItemAffixAreasWithOcrResults()
        {
            // Add current affix value
            for (int i = 0; i < _currentTooltip.ItemAffixAreas.Count; i++)
            {
                _currentTooltip.ItemAffixAreas[i].AffixValue = _currentTooltip.OcrResultAffixes[i].OcrResult.TextValue;
                _currentTooltip.ItemAffixAreas[i].AffixThresholdValue = _affixManager.GetAffixMinimalValue(_currentTooltip.OcrResultAffixes[i].OcrResult.AffixId);
            }
        }

        /// <summary>
        /// Search the current item tooltip for affixes.
        /// </summary>
        /// <returns>True when item affixes are found.</returns>
        private bool FindItemAffixes()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            // Delete tempered affixes areas and locations when disabled
            if (!_settingsManager.Settings.IsTemperedAffixDetectionEnabled)
            {
                _currentTooltip.ItemAffixAreas.RemoveAll(a => a.AffixType.Equals(Constants.AffixTypeConstants.Tempered));
                _currentTooltip.ItemAffixLocations.RemoveAll(l => l.Name.Contains("_temper_"));
            }

            // Delete non-rune affix areas and locations for rune types.
            if (_currentTooltip.ItemType.Equals(ItemTypeConstants.Rune))
            {
                _currentTooltip.ItemAffixAreas.RemoveAll(a => !a.AffixType.Equals(Constants.AffixTypeConstants.Rune));
                _currentTooltip.ItemAffixLocations.RemoveAll(l => !l.Name.Contains("_rune_"));
            }

            // Create image for each area
            var currentScreenTooltip = _currentScreenTooltipFilter.Convert<Bgr, byte>();
            var areaImages = new List<Image<Gray, byte>>();
            foreach (var area in _currentTooltip.ItemAffixAreas)
            {
                areaImages.Add(_currentScreenTooltipFilter.Copy(area.Location));
            }

            ConcurrentBag<ItemAffixDescriptor> itemAffixBag = new ConcurrentBag<ItemAffixDescriptor>();
            Parallel.For(0, areaImages.Count, index =>
            {
                var itemAffixResult = FindItemAffix(areaImages[index], index, _currentTooltip.ItemType);
                itemAffixBag.Add(itemAffixResult);
            });

            foreach (var itemAffix in itemAffixBag)
            {
                // Ignore "Revives allowed" and "Monster level" for sigils.
                if (_currentTooltip.ItemType.Contains(ItemTypeConstants.Sigil, StringComparison.OrdinalIgnoreCase) &&
                    (itemAffix.ItemAffix.Id.Equals("ItemDungeonAffixResses", StringComparison.OrdinalIgnoreCase) || itemAffix.ItemAffix.Id.Equals("MonsterLevel", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                _currentTooltip.ItemAffixes.Add(new Tuple<int, ItemAffix>(itemAffix.AreaIndex, itemAffix.ItemAffix));
            }

            // OCR results
            _currentTooltip.OcrResultAffixes.Sort((x, y) =>
            {
                return x.AreaIndex < y.AreaIndex ? -1 : x.AreaIndex > y.AreaIndex ? 1 : 0;
            });

            if (IsDebugInfoEnabled)
            {
                _eventAggregator.GetEvent<ScreenProcessItemAffixesOcrReadyEvent>().Publish(new ScreenProcessItemAffixesOcrReadyEventParams
                {
                    OcrResults = _currentTooltip.OcrResultAffixes
                });
            }

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

            OcrResultAffix ocrResult = 
                itemType.Equals(ItemTypeConstants.Sigil) ? _ocrHandler.ConvertToSigil(rawText) :
                itemType.Equals(ItemTypeConstants.Rune) ? _ocrHandler.ConvertToRune(rawText) : 
                _ocrHandler.ConvertToAffix(rawText);
            ocrResultDescriptor.OcrResult = ocrResult;

            ItemAffix itemAffix = 
                itemType.Equals(ItemTypeConstants.Sigil) ? _affixManager.GetSigil(ocrResult.AffixId, _currentTooltip.ItemType) :
                itemType.Equals(ItemTypeConstants.Rune) ? _affixManager.GetRune(ocrResult.AffixId, _currentTooltip.ItemType) :
                _affixManager.GetAffix(ocrResult.AffixId, _currentTooltip.ItemAffixAreas[index].AffixType, _currentTooltip.ItemType);
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

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            var currentScreenTooltipFilter = _currentScreenTooltipFilter.Copy(new Rectangle(0, 0, _currentScreenTooltip.Width / 7, _currentScreenTooltip.Height));
            Image<Bgr, byte>? currentScreenTooltip = IsDebugInfoEnabled ? currentScreenTooltipFilter.Convert<Bgr, byte>() : null;

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
                _currentTooltip.IsUniqueItem = itemAspectLocation.Name.Contains("_unique", StringComparison.OrdinalIgnoreCase) ||
                    itemAspectLocation.Name.Contains("_mythic", StringComparison.OrdinalIgnoreCase);

                if (IsDebugInfoEnabled)
                {
                    CvInvoke.Rectangle(currentScreenTooltip, itemAspectLocation.Location, new MCvScalar(0, 0, 255), 2);
                }

                // Skip foreach after the first valid aspect location is found.
                break;
            }

            if (IsDebugInfoEnabled)
            {
                _eventAggregator.GetEvent<ScreenProcessItemAspectLocationReadyEvent>().Publish(new ScreenProcessItemAspectLocationReadyEventParams
                {
                    ProcessedScreen = currentScreenTooltip.ToBitmap()
                });
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["AspectLocations"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !_currentTooltip.ItemAspectLocation.IsEmpty;
        }

        private ItemAspectLocationDescriptor FindItemAspectLocation(Image<Gray, byte> currentTooltip, string currentItemAspectLocation)
        {
            //_logger.LogDebug(string.Empty);
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

                //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAspectLocation}) Similarity: {String.Format("{0:0.0000000000}", similarity)}");

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

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            // The width of the image to detect the aspect-location is used to set the offset for the x-axis.
            int offsetAffixMarker = 0;
            if (_imageListItemAspectLocations.Keys.Any())
            {
                var affixMarkerImageName = _imageListItemAspectLocations.Keys.ToList()[0];
                offsetAffixMarker = (int)(_imageListItemAspectLocations[affixMarkerImageName].Width * 1.2);
            }

            List<Rectangle> areaSplitPoints = new List<Rectangle>();
            // Affix locations
            areaSplitPoints.AddRange(_currentTooltip.ItemAffixLocations.Select(loc => loc.Location));
            // Aspect location
            if (!_currentTooltip.ItemAspectLocation.IsEmpty) areaSplitPoints.Add(_currentTooltip.ItemAspectLocation);
            // Socket location
            if (_currentTooltip.ItemSocketLocations.Count > 0)
            {
                // An offset for the socket location is used because the ROI to look for sockets does not start at the top of the tooltip but after the aspect location.
                int offsetY = _currentTooltip.ItemAspectLocation.IsEmpty ? 0 : _currentTooltip.ItemAspectLocation.Y;

                areaSplitPoints.Add(new Rectangle(
                    _currentTooltip.ItemSocketLocations[0].X,
                    _currentTooltip.ItemSocketLocations[0].Y + offsetY,
                    _currentTooltip.ItemSocketLocations[0].Width,
                    _currentTooltip.ItemSocketLocations[0].Height));
            }
            // Splitter locations
            areaSplitPoints.AddRange(_currentTooltip.ItemSplitterLocations.Select(s => s.Location));

            // Sort all point on their y-coordinates.
            areaSplitPoints.Sort((x, y) =>
            {
                return x.Top < y.Top ? -1 : x.Top > y.Top ? 1 : 0;
            });

            // Create aspect area
            var splitterLocation = areaSplitPoints.FirstOrDefault(loc => loc.Y > _currentTooltip.ItemAspectLocation.Y);
            int yCoordsNextPoint = (splitterLocation.IsEmpty) ? _currentTooltip.Location.Height : splitterLocation.Y - _settingsManager.Settings.AffixAreaHeightOffsetBottom;

            _currentTooltip.ItemAspectArea = new Rectangle(
                _currentTooltip.ItemAspectLocation.X + offsetAffixMarker,
                _currentTooltip.ItemAspectLocation.Y - _settingsManager.Settings.AspectAreaHeightOffsetTop,
                _currentTooltip.Location.Width - _currentTooltip.ItemAspectLocation.X - offsetAffixMarker - _settingsManager.Settings.AffixAspectAreaWidthOffset,
                yCoordsNextPoint - (_currentTooltip.ItemAspectLocation.Y - _settingsManager.Settings.AspectAreaHeightOffsetTop));

            if (IsDebugInfoEnabled)
            {
                var currentScreenTooltip = _currentScreenTooltipFilter.Convert<Bgr, byte>();
                CvInvoke.Rectangle(currentScreenTooltip, _currentTooltip.ItemAspectArea, new MCvScalar(0, 0, 255), 2);

                _eventAggregator.GetEvent<ScreenProcessItemAspectAreaReadyEvent>().Publish(new ScreenProcessItemAspectAreaReadyEventParams
                {
                    ProcessedScreen = currentScreenTooltip.ToBitmap()
                });
            }

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

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            var area = _currentScreenTooltipFilter.Copy(_currentTooltip.ItemAspectArea);

            var itemAspectResult = FindItemAspect(area, _currentTooltip.ItemType);
            _currentTooltip.ItemAspect = itemAspectResult.ItemAspect;

            // OCR results
            if (IsDebugInfoEnabled)
            {
                _eventAggregator.GetEvent<ScreenProcessItemAspectOcrReadyEvent>().Publish(new ScreenProcessItemAspectOcrReadyEventParams
                {
                    OcrResult = _currentTooltip.OcrResultAspect
                });
            }

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
            OcrResultAffix ocrResult = _ocrHandler.ConvertToAspect(rawText);
            itemAspectResult.ItemAspect = _affixManager.GetAspect(ocrResult.AffixId, itemType);

            _currentTooltip.OcrResultAspect = ocrResult;

            return itemAspectResult;
        }

        /// <summary>
        /// Search the current item tooltip for the unique aspect.
        /// </summary>
        /// <returns>True when item aspect is found.</returns>
        private bool FindItemUniqueAspects()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            var area = _currentScreenTooltipFilter.Copy(_currentTooltip.ItemAspectArea);

            var itemAspectResult = FindItemUniqueAspect(area, _currentTooltip.ItemType);
            _currentTooltip.ItemAspect = itemAspectResult.ItemAspect;

            // OCR results
            if (IsDebugInfoEnabled)
            {
                _eventAggregator.GetEvent<ScreenProcessItemAspectOcrReadyEvent>().Publish(new ScreenProcessItemAspectOcrReadyEventParams
                {
                    OcrResult = _currentTooltip.OcrResultAspect
                });
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["Aspects"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !string.IsNullOrEmpty(_currentTooltip.ItemAspect.Id);
        }

        private ItemAspectDescriptor FindItemUniqueAspect(Image<Gray, byte> areaImageSource, string itemType)
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            ItemAspectDescriptor itemUniqueAspectResult = new ItemAspectDescriptor();
            string rawText = _ocrHandler.ConvertToText(areaImageSource.ToBitmap());
            OcrResultAffix ocrResult = _ocrHandler.ConvertToUnique(rawText);
            itemUniqueAspectResult.ItemAspect = _affixManager.GetUnique(ocrResult.AffixId, itemType);

            _currentTooltip.OcrResultAspect = ocrResult;

            return itemUniqueAspectResult;
        }

        /// <summary>
        /// Search the current item tooltip for the socket location.
        /// </summary>
        /// <returns>True when item aspect is found.</returns>
        private bool FindItemSocketLocations()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            // Reduce search area
            int offsetY = _currentTooltip.ItemAspectLocation.IsEmpty ? 0 : _currentTooltip.ItemAspectLocation.Y;

            var currentScreenTooltipFilter = _currentScreenTooltipFilter.Copy(new Rectangle(0, offsetY, _currentScreenTooltip.Width / 5, _currentScreenTooltip.Height - offsetY));
            Image<Bgr, byte>? currentScreenTooltip = IsDebugInfoEnabled ? currentScreenTooltipFilter.Convert<Bgr, byte>() : null;

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

                if (IsDebugInfoEnabled)
                {
                    CvInvoke.Rectangle(currentScreenTooltip, itemSocketLocation.Location, new MCvScalar(0, 0, 255), 2);
                }
            }

            if (IsDebugInfoEnabled)
            {
                _eventAggregator.GetEvent<ScreenProcessItemSocketLocationsReadyEvent>().Publish(new ScreenProcessItemSocketLocationsReadyEventParams
                {
                    ProcessedScreen = currentScreenTooltip.ToBitmap()
                });
            }

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

        /// <summary>
        /// Search the current item tooltip for the splitter locations.
        /// </summary>
        /// <returns>True when splitter is found.</returns>
        private bool FindItemSplitterLocations()
        {
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            bool IsDebugInfoEnabled = _settingsManager.Settings.IsDebugInfoEnabled;

            int roiWidth = _currentScreenTooltip.Width / 7;
            int roiStartX = (_currentScreenTooltip.Width / 2) - (roiWidth / 2);
            var currentScreenTooltipFilter = _currentScreenTooltipFilter.Copy(new Rectangle(roiStartX, 0, roiWidth, _currentScreenTooltip.Height));
            Image<Bgr, byte>? currentScreenTooltip = IsDebugInfoEnabled ? currentScreenTooltipFilter.Convert<Bgr, byte>() : null;

            ConcurrentBag<ItemSplitterLocationDescriptor> itemSplitterLocationBag = new ConcurrentBag<ItemSplitterLocationDescriptor>();
            Parallel.ForEach(_imageListItemSplitterLocations.Keys, itemSplitterLocation =>
            {
                var itemSplitterLocations = FindItemSplitterLocation(currentScreenTooltipFilter, itemSplitterLocation);
                itemSplitterLocations.ForEach(itemSplitterLocationBag.Add);
            });

            // Sort results by Y-Pos
            var itemSplitterLocations = itemSplitterLocationBag.ToList();
            itemSplitterLocations.Sort((x, y) =>
            {
                return x.Location.Top < y.Location.Top ? -1 : x.Location.Top > y.Location.Top ? 1 : 0;
            });

            // Clean item splitter locations
            // This removes dot-splitter locations that matched at the same location as the first dot-splitter_top
            var splittersTop = itemSplitterLocations.FindAll(s => s.Name.Contains("dot-splitter_top"));
            if (splittersTop.Count > 0)
            {
                int splitterTopY = splittersTop[0].Location.Y;
                itemSplitterLocations.RemoveAll(s => !s.Name.Contains("dot-splitter_top") && s.Location.Y - 2 <= splitterTopY && splitterTopY <= s.Location.Y + 2);
            }

            foreach (var itemSplitterLocation in itemSplitterLocations)
            {
                _currentTooltip.ItemSplitterLocations.Add(itemSplitterLocation);
                if (itemSplitterLocation.Name.Contains("dot-splitter_top", StringComparison.OrdinalIgnoreCase))
                {
                    _currentTooltip.HasTooltipTopSplitter = true;
                }

                if (IsDebugInfoEnabled)
                {
                    CvInvoke.Rectangle(currentScreenTooltip, itemSplitterLocation.Location, new MCvScalar(0, 0, 255), 2);
                }
            }

            if (IsDebugInfoEnabled)
            {
                _eventAggregator.GetEvent<ScreenProcessItemSplitterLocationsReadyEvent>().Publish(new ScreenProcessItemSplitterLocationsReadyEventParams
                {
                    ProcessedScreen = currentScreenTooltip.ToBitmap()
                });
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _currentTooltip.PerformanceResults["SplitterLocations"] = (int)elapsedMs;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return _currentTooltip.ItemSplitterLocations.Any();
        }

        private List<ItemSplitterLocationDescriptor> FindItemSplitterLocation(Image<Gray, byte> currentTooltipSource, string currentItemSplitterLocation)
        {
            //_logger.LogDebug(string.Empty);
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");

            List<ItemSplitterLocationDescriptor> itemSplitterLocations = new List<ItemSplitterLocationDescriptor>();
            Image<Gray, byte> currentTooltip;
            Image<Gray, byte> currentItemSplitterLocationImage;
            int counter = 0;
            double similarity = 0.0;
            Point location = Point.Empty;

            try
            {
                lock (_lockCloneImage)
                {
                    currentTooltip = currentTooltipSource.Clone();
                    currentItemSplitterLocationImage = _imageListItemSplitterLocations[currentItemSplitterLocation].Clone();
                }

                do
                {
                    counter++;

                    (similarity, location) = FindMatchTemplate(currentTooltip, currentItemSplitterLocationImage);

                    //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemSplitterLocation}) Similarity: {String.Format("{0:0.0000000000}", similarity)}");

                    if (similarity < _settingsManager.Settings.ThresholdSimilaritySplitterLocation)
                    {
                        itemSplitterLocations.Add(new ItemSplitterLocationDescriptor
                        {
                            Similarity = similarity,
                            Location = new Rectangle(location, currentItemSplitterLocationImage.Size),
                            Name = currentItemSplitterLocation
                        });
                    }

                    // Mark location so that it's only detected once.
                    var rectangle = new Rectangle(location, currentItemSplitterLocationImage.Size);
                    CvInvoke.Rectangle(currentTooltip, rectangle, new MCvScalar(255, 255, 255), -1);

                } while (similarity < _settingsManager.Settings.ThresholdSimilaritySplitterLocation && counter < 20);

                if (counter >= 20)
                {
                    _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                    {
                        Message = $"Too many splitter locations found in tooltip. Aborted! Check images in debug view."
                    });
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }

            return itemSplitterLocations;
        }

        private void SetMultiBuildMode()
        {
            foreach (MultiBuild multiBuild in _settingsManager.Settings.MultiBuildList)
            {
                _currentTooltip.ItemAffixesBuildList.Add(new List<Tuple<int, ItemAffix>>());
                _currentTooltip.ItemAspectBuildList.Add(new ItemAffix());
                SetMultiBuildMode(_currentTooltip.ItemAffixesBuildList[multiBuild.Index], _currentTooltip.ItemAspectBuildList[multiBuild.Index], multiBuild.Name, multiBuild.Color);
            }
        }

        private void SetMultiBuildMode(List<Tuple<int, ItemAffix>> buildAffixes, ItemAffix buildAspect, string buildName, System.Windows.Media.Color buildColor)
        {
            if (!string.IsNullOrWhiteSpace(buildName))
            {
                var preset = _affixManager.AffixPresets.FirstOrDefault(preset => preset.Name.Equals(buildName));
                if (preset == null) return;

                // Affixes
                System.Windows.Media.Color color = Colors.Red;
                foreach (var currentItemAffix in _currentTooltip.ItemAffixes)
                {
                    color = Colors.Red;
                    bool isAnyType = false;
                    bool isGreater = false;
                    bool isImplicit = false;
                    bool isTempered = false;
                    if (currentItemAffix.Item2.Type.Equals(ItemTypeConstants.Sigil))
                    {
                        var affix = preset.ItemSigils.FirstOrDefault(a => a.Id.Equals(currentItemAffix.Item2.Id) && a.Type.Equals(currentItemAffix.Item2.Type));
                        if (affix != null)
                        {
                            color = buildColor;
                        }
                    }
                    else if(currentItemAffix.Item2.Type.Equals(ItemTypeConstants.Rune))
                    {
                        var rune = preset.ItemRunes.FirstOrDefault(a => a.Id.Equals(currentItemAffix.Item2.Id));
                        if (rune != null)
                        {
                            color = buildColor;
                        }
                    }
                    else
                    {
                        bool isImplicitArea = _currentTooltip.ItemAffixAreas[currentItemAffix.Item1].AffixType.Equals(Constants.AffixTypeConstants.Implicit);
                        bool isTemperedArea = _currentTooltip.ItemAffixAreas[currentItemAffix.Item1].AffixType.Equals(Constants.AffixTypeConstants.Tempered);
                        var affix = preset.ItemAffixes.FirstOrDefault(a => a.Id.Equals(currentItemAffix.Item2.Id) && a.Type.Equals(_currentTooltip.ItemType) && a.IsImplicit == isImplicitArea && a.IsTempered == isTemperedArea);
                        if (affix == null)
                        {
                            // Check if the affix is set to accept any item type.
                            affix = preset.ItemAffixes.FirstOrDefault(a => a.Id.Equals(currentItemAffix.Item2.Id));
                            affix = affix?.IsAnyType ?? false ? affix : null;
                        }

                        if (affix != null)
                        {
                            color = buildColor;
                            isAnyType = affix.IsAnyType;
                            isGreater = affix.IsGreater;
                            isImplicit = affix.IsImplicit;
                            isTempered = affix.IsTempered;
                        }
                    }

                    buildAffixes.Add(new Tuple<int, ItemAffix>(currentItemAffix.Item1, new ItemAffix
                    {
                        Id = currentItemAffix.Item2.Id.Substring(0),
                        Type = _currentTooltip.ItemType,
                        Color = color,
                        IsAnyType = isAnyType,
                        IsGreater = isGreater,
                        IsImplicit = isImplicit,
                        IsTempered = isTempered
                    }));
                }

                // Aspect
                if (!string.IsNullOrWhiteSpace(_currentTooltip.ItemAspect.Id))
                {
                    color = Colors.Red;

                    if (_settingsManager.Settings.IsAspectDetectionEnabled && !_currentTooltip.IsUniqueItem)
                    {
                        var aspect = preset.ItemAspects.FirstOrDefault(a => a.Id.Equals(_currentTooltip.ItemAspect.Id) && a.Type.Equals(_currentTooltip.ItemAspect.Type));
                        if (aspect != null)
                        {
                            color = buildColor;
                        }
                    }
                    else if (_settingsManager.Settings.IsUniqueDetectionEnabled && _currentTooltip.IsUniqueItem)
                    {
                        var unique = preset.ItemUniques.FirstOrDefault(a => a.Id.Equals(_currentTooltip.ItemAspect.Id));
                        if (unique != null)
                        {
                            color = buildColor;
                        }
                    }

                    buildAspect.Id = _currentTooltip.ItemAspect.Id.Substring(0);
                    buildAspect.Type = _currentTooltip.ItemAspect.Type.Substring(0);
                    buildAspect.Color = color;
                }
            }
        }

        #endregion
    }
}
