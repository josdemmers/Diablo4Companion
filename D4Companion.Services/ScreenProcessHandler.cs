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
        private readonly ISettingsManager _settingsManager;
        private readonly IAffixPresetManager _affixPresetManager;

        private Image<Bgr, byte> _currentScreenTooltip;
        private Image<Gray, byte> _currentScreenTooltipBin;
        private Gray _thresholdMin, _thresholdMax;
        private ItemTooltipDescriptor _currentTooltip = new ItemTooltipDescriptor();
        Dictionary<string, Image<Gray, byte>> _imageListItemTooltips = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemTypes = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemTypesLite = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixes = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAspectLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAspects = new Dictionary<string, Image<Gray, byte>>();
        private bool _isEnabled = false;
        private object _lockCloneImage = new object();
        private Task? _processTask = null;

        #region Constructors

        public ScreenProcessHandler(IEventAggregator eventAggregator, ILogger<ScreenProcessHandler> logger, ISettingsManager settingsManager, IAffixPresetManager affixPresetManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ScreenCaptureReadyEvent>().Subscribe(HandleScreenCaptureReadyEvent);
            _eventAggregator.GetEvent<SystemPresetChangedEvent>().Subscribe(HandleSettingsChangedEvent);
            _eventAggregator.GetEvent<ThresholdsChangedEvent>().Subscribe(HandleSettingsChangedEvent);
            _eventAggregator.GetEvent<ToggleOverlayEvent>().Subscribe(HandleToggleOverlayEvent);
            _eventAggregator.GetEvent<ToggleOverlayFromGUIEvent>().Subscribe(HandleToggleOverlayFromGUIEvent);

            // Init logger
            _logger = logger;

            // Init services
            _affixPresetManager = affixPresetManager;
            _settingsManager = settingsManager;

            // Init image list.
            ReloadImageList();
        }

        #endregion

        #region Events

        #endregion

        #region Properties

        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }

        #endregion

        #region Event handlers

        private void HandleScreenCaptureReadyEvent(ScreenCaptureReadyEventParams screenCaptureReadyEventParams)
        {
            if (!IsEnabled) return;
            if (_processTask != null && (_processTask.Status.Equals(TaskStatus.Running) || _processTask.Status.Equals(TaskStatus.WaitingForActivation))) return;
            if (screenCaptureReadyEventParams.CurrentScreen == null) return;

            _processTask?.Dispose();
            _processTask = Task.Run(() => ProcessScreen(screenCaptureReadyEventParams.CurrentScreen));
        }

        private void HandleSettingsChangedEvent()
        {
            ReloadImageList();
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

        #region Methods

        private void ReloadImageList()
        {
            _imageListItemTooltips.Clear();
            _imageListItemTypes.Clear();
            _imageListItemTypesLite.Clear();
            _imageListItemAffixLocations.Clear();
            _imageListItemAffixes.Clear();
            _imageListItemAspectLocations.Clear();
            _imageListItemAspects.Clear();

            (_thresholdMin, _thresholdMax) = (new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            var baseDirectory = new DirectoryInfo(_settingsManager.Settings.SelectedSystemPresetDir);

            if (!baseDirectory.Exists)
            {
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"System preset not found at \"{baseDirectory.FullName}\". Go to settings to select one."
                });
                return;
            }

            void LoadDirectoryTo(string? subDirectory, Dictionary<string, Image<Gray, byte>> target, Func<string, bool>? additionalFilter = null, bool threshold = true)
            {
                var directory = subDirectory != null && baseDirectory.GetDirectories(subDirectory).FirstOrDefault() is DirectoryInfo info ? info : baseDirectory;

                var files = directory.GetFiles("*.png").Select(file => new { Name = file.Name[..^file.Extension.Length].ToLower(), file.FullName });

                if (additionalFilter != null) files = files.Where(file => additionalFilter(file.Name));

                foreach (var file in files)
                {
                    var image = new Image<Gray, byte>(file.FullName);

                    if (threshold) image = image.ThresholdBinaryInv(_thresholdMin, _thresholdMax);

                    target.TryAdd(file.Name, image);
                }
            }

            LoadDirectoryTo("Tooltips", _imageListItemTooltips, threshold: false);
            LoadDirectoryTo("Types", _imageListItemTypes, name => name != "weapon_all");
            LoadDirectoryTo("Types", _imageListItemTypesLite, name => name == "weapon_all" || (!name.StartsWith("weapon_") && !name.StartsWith("ranged_") && !name.StartsWith("offhand_focus")));

            LoadDirectoryTo(null, _imageListItemAffixLocations, name => name.StartsWith("dot-affixes_"));
            LoadDirectoryTo("Affixes", _imageListItemAffixes);

            LoadDirectoryTo(null, _imageListItemAspectLocations, name => name.StartsWith("dot-aspects_"));
            LoadDirectoryTo("Aspects", _imageListItemAspects);
        }

        [LogTime]
        [LogError]
        private void ProcessScreen(Bitmap currentScreen)
        {
            _currentTooltip = new ItemTooltipDescriptor();

            if (currentScreen.Height < 50)
            {
                _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Diablo IV window is probably minimized.");
                return;
            }

            bool result = FindTooltips(currentScreen);
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
                FindItemAffixes();

                result = FindItemAspectLocations();
            }
            if (result)
            {
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

        private static void DrawRect<TColor>(Image<TColor, byte> target, Rectangle rect) where TColor : struct, IColor
        {
            CvInvoke.Rectangle(target, rect, new MCvScalar(0, 0, 255));
        }

        private static void FillRect<TColor>(Image<TColor, byte> target, Rectangle rect, MCvScalar color) where TColor : struct, IColor
        {
            CvInvoke.Rectangle(target, rect, color, -1);
        }

        private static (double Similarity, Point Locaiton) Find(Image<Gray, byte> image, Image<Gray, byte> target)
        {
            var minVal = 0.0;
            var maxVal = 0.0;
            var minLoc = new Point();
            var maxLoc = new Point();
            var mat = new Mat();

            CvInvoke.MatchTemplate(image, target, mat, TemplateMatchingType.SqdiffNormed);
            CvInvoke.MinMaxLoc(mat, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

            return (minVal, minLoc);
        }

        [LogTime]
        private bool FindTooltips(Bitmap currentScreen)
        {
            var currentScreenImg = currentScreen.ToImage<Bgr, byte>();
            var currentScreenGray = currentScreenImg.Convert<Gray, byte>();
            var itemTooltipBag = new ConcurrentBag<ItemTooltipDescriptor>();

            Parallel.ForEach(_imageListItemTooltips.Keys, itemTooltip =>
            {
                var toolTip = FindTooltip(currentScreenGray, itemTooltip);
                
                if (toolTip != null) itemTooltipBag.Add(toolTip);
            });

            var foundToolTip = default(ItemTooltipDescriptor?);

            foreach (var itemTooltip in itemTooltipBag)
            {
                if (foundToolTip == null || foundToolTip.Similarity < itemTooltip.Similarity)
                    foundToolTip = itemTooltip;
            }

            var processedScreen = currentScreen;

            if (foundToolTip != null)
            {
                _currentTooltip.Location = foundToolTip.Location;
                _currentScreenTooltip = currentScreenImg.Copy(_currentTooltip.Location);
                _currentScreenTooltipBin = _currentScreenTooltip
                    .Convert<Gray, byte>()
                    .ThresholdBinaryInv(_thresholdMin, _thresholdMax);

                DrawRect(currentScreenImg, foundToolTip.Location);
            }

            _eventAggregator.GetEvent<ScreenProcessItemTooltipReadyEvent>().Publish(new ScreenProcessItemTooltipReadyEventParams
            {
                ProcessedScreen = currentScreenImg.ToBitmap()
            });

            return foundToolTip != null;
        }

        [LogError]
        private ItemTooltipDescriptor? FindTooltip(Image<Gray, byte> currentScreen, string currentItemTooltip)
        {
            Image<Gray, byte> currentItemTooltipImage;

            lock (_lockCloneImage)
            {
                currentItemTooltipImage = _imageListItemTooltips[currentItemTooltip].Clone();
            }

            var (similairy, location) = Find(currentScreen, currentItemTooltipImage);

            return similairy < _settingsManager.Settings.ThresholdSimilarityTooltip
                ? new ItemTooltipDescriptor()
                {
                    Similarity = similairy,
                    Location = new Rectangle(new Point(location.X, 0), new Size(_settingsManager.Settings.TooltipWidth, location.Y))
                }
                : null;
        }

        [LogTime]
        private bool FindItemTypes()
        {
            var itemTypeBag = new ConcurrentBag<ItemTypeDescriptor>();
            var itemTypeKeys = _settingsManager.Settings.LiteMode ? _imageListItemTypesLite.Keys : _imageListItemTypes.Keys;

            Parallel.ForEach(itemTypeKeys, itemType =>
            {
                var foundType = FindItemType(_currentScreenTooltipBin, itemType);

                if (foundType != null) itemTypeBag.Add(foundType);
            });

            var foundItemType = default(ItemTypeDescriptor?);

            foreach (var itemType in itemTypeBag)
            {
                if (foundItemType == null || foundItemType.Similarity > itemType.Similarity)
                    foundItemType = itemType;
            }

            var currentScreenTooltipGui = _currentScreenTooltipBin;

            if (foundItemType != null)
            {
                _currentTooltip.ItemType = foundItemType.Name;

                currentScreenTooltipGui = _currentScreenTooltipBin.Clone();
                DrawRect(currentScreenTooltipGui, foundItemType.Location);
            }

            _eventAggregator.GetEvent<ScreenProcessItemTypeReadyEvent>().Publish(new ScreenProcessItemTypeReadyEventParams
            {
                ProcessedScreen = currentScreenTooltipGui.ToBitmap()
            });

            return foundItemType != null;
        }

        [LogError]
        private ItemTypeDescriptor? FindItemType(Image<Gray, byte> currentTooltip, string currentItemType)
        {
            Image<Gray, byte> currentItemTypeImage;

            lock (_lockCloneImage)
            {
                currentItemTypeImage = _settingsManager.Settings.LiteMode ? _imageListItemTypesLite[currentItemType] : _imageListItemTypes[currentItemType];
            }

            var (similarity, location) = Find(currentTooltip, currentItemTypeImage);

            return similarity < _settingsManager.Settings.ThresholdSimilarityType
                ? new ItemTypeDescriptor()
                {
                    Name = currentItemType,
                    Similarity = similarity,
                    Location = new Rectangle(location, currentItemTypeImage.Size)
                }
                : null;
        }

        [LogTime]
        private bool FindItemAffixLocations()
        {
            var itemAffixLocationBag = new ConcurrentBag<ItemAffixLocationDescriptor>();

            Parallel.ForEach(_imageListItemAffixLocations.Keys, itemAffixLocation =>
            {
                var foundLocations = FindItemAffixLocation(_currentScreenTooltipBin, Path.GetFileNameWithoutExtension(itemAffixLocation));

                if (foundLocations != null) foundLocations.ForEach(itemAffixLocationBag.Add);
            });

            var processedScreen = _currentScreenTooltipBin;

            if (itemAffixLocationBag.Any())
            {
                var currentScreenTooltipGui = _currentScreenTooltipBin.Clone();
                _currentTooltip.ItemAffixLocations.Clear();

                foreach (var itemAffixLocation in itemAffixLocationBag)
                {
                    _currentTooltip.ItemAffixLocations.Add(itemAffixLocation.Location);
                    DrawRect(currentScreenTooltipGui, itemAffixLocation.Location);
                }

                processedScreen = currentScreenTooltipGui;
            }

            _eventAggregator.GetEvent<ScreenProcessItemAffixLocationsReadyEvent>().Publish(new ScreenProcessItemAffixLocationsReadyEventParams
            {
                ProcessedScreen = processedScreen.ToBitmap()
            });

            return itemAffixLocationBag.Any();
        }

        [LogError]
        private List<ItemAffixLocationDescriptor>? FindItemAffixLocation(Image<Gray, byte> currentTooltip, string currentItemAffixLocation)
        {
            Image<Gray, byte> currentTooltipImage, currentItemAffixLocationImage;

            lock (_lockCloneImage)
            {
                currentTooltipImage = currentTooltip.Clone();
                currentItemAffixLocationImage = _imageListItemAffixLocations[currentItemAffixLocation].Clone();
            }

            var counter = 0;
            var similarity = .0;
            var location = Point.Empty;

            var itemAffixLocations = default(List<ItemAffixLocationDescriptor>?);
            do
            {
                counter++;

                (similarity, location) = Find(currentTooltipImage, currentItemAffixLocationImage);

                var rectangle = new Rectangle(location, currentItemAffixLocationImage.Size);

                // Too many similarities. Need to add some constraints to filter out false positives.
                if (location.X < currentTooltipImage.Width / 7 && similarity < _settingsManager.Settings.ThresholdSimilarityAffixLocation)
                {
                    if (itemAffixLocations == null) itemAffixLocations = new List<ItemAffixLocationDescriptor>();

                    itemAffixLocations.Add(new ItemAffixLocationDescriptor
                    {
                        Similarity = similarity,
                        Location = rectangle
                    });
                }

                // Mark location so that it's only detected once.
                FillRect(currentTooltipImage, rectangle, color: new MCvScalar(255, 255, 255));
            } while (similarity < _settingsManager.Settings.ThresholdSimilarityAffixLocation && counter < 20);

            if (counter >= 20)
            {
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"Too many affix locations found in tooltip. Aborted! Check images in debug view."
                });
            }

            return itemAffixLocations;
        }

        [LogTime]
        private bool FindItemAffixes()
        {
            var affixPreset = _settingsManager.Settings.SelectedAffixName;
            var itemAffixes = _affixPresetManager.AffixPresets.FirstOrDefault(s => s.Name == affixPreset)?.ItemAffixes;
            var itemAffixesPerType = itemAffixes?.FindAll(itemAffix => _currentTooltip.ItemType.StartsWith($"{itemAffix.Type}_"));
            var processedScreen = _currentScreenTooltipBin;

            if (itemAffixesPerType != null) 
            {
                ConcurrentBag<ItemAffixDescriptor> itemAffixBag = new ConcurrentBag<ItemAffixDescriptor>();
                Parallel.ForEach(itemAffixesPerType, itemAffix =>
                {
                    var foundAffixes = FindItemAffix(_currentScreenTooltipBin, Path.GetFileNameWithoutExtension(itemAffix.FileName));

                    if (foundAffixes != null)
                    {
                        foundAffixes.ForEach(itemAffixBag.Add);
                    }
                });

                if (itemAffixBag.Any())
                {
                    var currentScreenTooltipGui = _currentScreenTooltipBin.Clone();
                    _currentTooltip.ItemAffixes.Clear();

                    foreach (var itemAffix in itemAffixBag)
                    {
                        _currentTooltip.ItemAffixes.Add(itemAffix.Location);
                        DrawRect(currentScreenTooltipGui, itemAffix.Location);
                    }

                    processedScreen = currentScreenTooltipGui;
                }
            }

            _eventAggregator.GetEvent<ScreenProcessItemAffixesReadyEvent>().Publish(new ScreenProcessItemAffixesReadyEventParams
            {
                ProcessedScreen = processedScreen.ToBitmap()
            });

            return _currentTooltip.ItemAffixes.Any();
        }

        [LogError]
        private List<ItemAffixDescriptor>? FindItemAffix(Image<Gray, byte> currentTooltip, string currentItemAffix)
        {
            Image<Gray, byte> currentTooltipImage;
            Image<Gray, byte> currentItemAffixImage;

            lock (_lockCloneImage)
            {
                currentTooltipImage = currentTooltip.Clone();
                currentItemAffixImage = _imageListItemAffixes[currentItemAffix].Clone();
            }

            int counter = 0;
            var similarity = .0;
            var location = Point.Empty;
            var itemAffixes = default(List<ItemAffixDescriptor>?);

            do
            {
                counter++;

                (similarity, location) = Find(currentTooltipImage, currentItemAffixImage);

                var rectangle = new Rectangle(location, currentItemAffixImage.Size);

                // Note: Ignore minVal == 0 results. Looks like they are random false positives. Requires more testing
                // Unfortunately also valid matches, can't ignore the results.
                if (similarity < _settingsManager.Settings.ThresholdSimilarityAffix)
                {
                    if (itemAffixes == null) itemAffixes = new List<ItemAffixDescriptor>();

                    itemAffixes.Add(new ItemAffixDescriptor
                    {
                        Similarity = similarity,
                        Location = rectangle
                    });
                }

                // Mark location so that it's only detected once.
                FillRect(currentTooltipImage, rectangle, color: new MCvScalar(255, 255, 255));
            } while (similarity < _settingsManager.Settings.ThresholdSimilarityAffix && counter < 10);

            if (counter >= 10)
            {
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"Too many affixes found in tooltip. Aborted! Check images in debug view."
                });
            }

            return itemAffixes;
        }

        [LogTime]
        private bool FindItemAspectLocations()
        {
            var currentScreenTooltipGui = _currentScreenTooltipBin;

            var itemAspectLocationBag = new ConcurrentBag<ItemAspectLocationDescriptor>();

            Parallel.ForEach(_imageListItemAspectLocations.Keys, itemAspectLocation =>
            {
                var aspectLocation = FindItemAspectLocation(_currentScreenTooltipBin, Path.GetFileNameWithoutExtension(itemAspectLocation));
                
                if (aspectLocation != null) itemAspectLocationBag.Add(aspectLocation);
            });

            var foundItemAspectLocation = default(ItemAspectLocationDescriptor?);

            foreach (var itemAspectLocation in itemAspectLocationBag)
            {
                if (foundItemAspectLocation == null || foundItemAspectLocation.Similarity > itemAspectLocation.Similarity)
                    foundItemAspectLocation = itemAspectLocation;
            }

            if (foundItemAspectLocation != null)
            {
                _currentTooltip.ItemAspect = foundItemAspectLocation.Location;

                currentScreenTooltipGui = _currentScreenTooltipBin.Clone();
                DrawRect(currentScreenTooltipGui, foundItemAspectLocation.Location);
            }

            _eventAggregator.GetEvent<ScreenProcessItemAspectLocationReadyEvent>().Publish(new ScreenProcessItemAspectLocationReadyEventParams
            {
                ProcessedScreen = currentScreenTooltipGui.ToBitmap()
            });

            return foundItemAspectLocation != null;
        }

        [LogError]
        private ItemAspectLocationDescriptor? FindItemAspectLocation(Image<Gray, byte> currentTooltip, string currentItemAspectLocation)
        {
            Image<Gray, byte> currentItemAspectImage;

            lock (_lockCloneImage) 
            {
                currentItemAspectImage = _imageListItemAspectLocations[currentItemAspectLocation].Clone();
            }

            var (similarity, location) = Find(currentTooltip, currentItemAspectImage);

            return similarity < _settingsManager.Settings.ThresholdSimilarityAspectLocation
                ? new ItemAspectLocationDescriptor()
                {
                    Similarity = similarity,
                    Location = new Rectangle(location, currentItemAspectImage.Size)
                }
                : null;
        }

        [LogTime]
        private bool FindItemAspects()
        {
            var currentScreenTooltipGui = _currentScreenTooltipBin;
            var affixPreset = _settingsManager.Settings.SelectedAffixName;
            var itemAspects = _affixPresetManager.AffixPresets.FirstOrDefault(s => s.Name == affixPreset)?.ItemAspects;
            var itemAspectsPerType = itemAspects?.FindAll(itemAspect => _currentTooltip.ItemType.StartsWith($"{itemAspect.Type}_"));
            var foundItemAspect = default(ItemAspectDescriptor?);

            if (itemAspectsPerType != null)
            {
                var itemAspectBag = new ConcurrentBag<ItemAspectDescriptor>();

                Parallel.ForEach(itemAspectsPerType, itemAspect =>
                {
                    var aspect = FindItemAspect(_currentScreenTooltipBin, Path.GetFileNameWithoutExtension(itemAspect.FileName));
                    
                    if (aspect != null) itemAspectBag.Add(aspect);
                });

                foreach (var itemAspect in itemAspectBag)
                {
                    if (foundItemAspect == null || foundItemAspect.Similarity > itemAspect.Similarity)
                        foundItemAspect = itemAspect;
                }

                if (foundItemAspect != null)
                {
                    _currentTooltip.ItemAspect = foundItemAspect.Location;

                    currentScreenTooltipGui = _currentScreenTooltipBin.Clone();
                    DrawRect(currentScreenTooltipGui, foundItemAspect.Location);
                }
            }

            _eventAggregator.GetEvent<ScreenProcessItemAspectReadyEvent>().Publish(new ScreenProcessItemAspectReadyEventParams
            {
                ProcessedScreen = currentScreenTooltipGui.ToBitmap()
            });

            return foundItemAspect != null;
        }

        [LogError]
        private ItemAspectDescriptor? FindItemAspect(Image<Gray, byte> currentTooltip, string currentItemAspect)
        {
            Image<Gray, byte> currentItemAspectImage;

            lock (_lockCloneImage)
            {
                currentItemAspectImage = _imageListItemAspects[currentItemAspect].Clone();
            }

            var (similarity, location) = Find(currentTooltip, currentItemAspectImage);

            return similarity < _settingsManager.Settings.ThresholdSimilarityAspect
                ? new ItemAspectDescriptor()
                {
                    Similarity = similarity,
                    Location = new Rectangle(location, currentItemAspectImage.Size)
                }
                : null;
        }

        #endregion
    }
}
