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
        private object _lockCloneImage = new object();
        private Task? _processTask = null;

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

            _currentScreen = screenCaptureReadyEventParams.CurrentScreen.ToImage<Bgr, byte>();
            _processTask?.Dispose();
            _processTask = StartProcessTask();
        }

        private void HandleSystemPresetChangedEvent()
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

            var systemPreset = _settingsManager.Settings.SelectedSystemPreset;
            var baseDirectory = new DirectoryInfo($"Images\\{systemPreset}\\");
            if (!baseDirectory.Exists)
            {
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"System preset not found at \"{baseDirectory.FullName}\". Go to settings to select one."
                });
                return;
            }

            void LoadDirectoryTo(string? subDirectory, Dictionary<string, Image<Gray, byte>> target, Func<string, bool>? additionalFilter = null)
            {
                var directory = subDirectory != null && baseDirectory.GetDirectories(subDirectory).FirstOrDefault() is DirectoryInfo info ? info : baseDirectory;

                var files = directory.GetFiles("*.png").Select(file => new { Name = file.Name[..^file.Extension.Length].ToLower(), file.FullName });

                if (additionalFilter != null) files = files.Where(file => additionalFilter(file.Name));

                foreach (var file in files)
                {
                    target.TryAdd(file.Name, new Image<Gray, byte>(file.FullName));
                }
            }

            LoadDirectoryTo("Tooltips", _imageListItemTooltips);
            LoadDirectoryTo("Types", _imageListItemTypes, name => name != "weapon_all");
            LoadDirectoryTo("Types", _imageListItemTypesLite, name => name == "weapon_all" || (!name.StartsWith("weapon_") && !name.StartsWith("ranged_") && !name.StartsWith("offhand_focus")));

            LoadDirectoryTo(null, _imageListItemAffixLocations, name => name.StartsWith("dot-affixes_"));
            LoadDirectoryTo("Affixes", _imageListItemAffixes);

            LoadDirectoryTo(null, _imageListItemAspectLocations, name => name.StartsWith("dot-aspects_"));
            LoadDirectoryTo("Aspects", _imageListItemAspects);
        }

        private async Task StartProcessTask()
        {
            await Task.Run(() =>
            {
                try
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();

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

                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Total Elapsed time: {elapsedMs}");

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
                }
            });
        }

        private bool FindTooltips()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();

            Image<Gray, byte> currentScreenFilter = new Image<Gray, byte>(currentScreen.Width, currentScreen.Height, new Gray(0));
            currentScreenFilter = currentScreen.Convert<Gray, byte>();

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
            ItemTooltipDescriptor tooltip = new ItemTooltipDescriptor();
            Mat result = new Mat();
            Image<Gray, byte> currentItemTooltipImage = new Image<Gray, byte>(0, 0);

            try
            {
                lock (_lockCloneImage)
                {
                    currentItemTooltipImage = _imageListItemTooltips[currentItemTooltip].Clone();
                }

                double minVal = 0.0;
                double maxVal = 0.0;
                Point minLoc = new Point();
                Point maxLoc = new Point();

                CvInvoke.MatchTemplate(currentScreen, currentItemTooltipImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

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

        private bool FindItemTypes()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Image<Gray, byte> currentScreenTooltipFilter = new Image<Gray, byte>(_currentScreenTooltip.Width, _currentScreenTooltip.Height, new Gray(0));
            currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>();
            currentScreenTooltipFilter = currentScreenTooltipFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin),new Gray(_settingsManager.Settings.ThresholdMax));

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
            ItemTypeDescriptor itemType = new ItemTypeDescriptor { Name = currentItemType };
            Mat result = new Mat();
            Image<Gray, byte> currentItemTypeImage = new Image<Gray, byte>(0, 0);

            try
            {
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

        private bool FindItemAffixLocations()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Image<Gray, byte> currentScreenTooltipFilter = new Image<Gray, byte>(_currentScreenTooltip.Width, _currentScreenTooltip.Height, new Gray(0));
            currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>();
            currentScreenTooltipFilter = currentScreenTooltipFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

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
            List<ItemAffixLocationDescriptor> itemAffixLocations = new List<ItemAffixLocationDescriptor>();
            Mat result = new Mat();
            Image<Gray, byte> currentTooltipImage = new Image<Gray, byte>(0, 0);
            Image<Gray, byte> currentItemAffixLocationImage = new Image<Gray, byte>(0, 0);

            try
            {
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

                    // Too many similarities. Need to add some constraints to filter out false positives.
                    if (minLoc.X < currentTooltipImage.Width / 7 && minVal < _settingsManager.Settings.ThresholdSimilarityAffixLocation)
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

        private bool FindItemAffixes()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Image<Gray, byte> currentScreenTooltipFilter = new Image<Gray, byte>(_currentScreenTooltip.Width, _currentScreenTooltip.Height, new Gray(0));
            currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>();
            currentScreenTooltipFilter = currentScreenTooltipFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

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
            List<ItemAffixDescriptor> itemAffixes = new List<ItemAffixDescriptor>();
            Mat result = new Mat();
            Image<Gray, byte> currentTooltipImage = new Image<Gray, byte>(0, 0);
            Image<Gray, byte> currentItemAffixImage = new Image<Gray, byte>(0, 0);

            try
            {
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

                    // Note: Ignore minVal == 0 results. Looks like they are random false positives. Requires more testing
                    // Unfortunately also valid matches, can't ignore the results.
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

        private bool FindItemAspectLocations()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Image<Gray, byte> currentScreenTooltipFilter = new Image<Gray, byte>(_currentScreenTooltip.Width, _currentScreenTooltip.Height, new Gray(0));
            currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>();

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
            ItemAspectLocationDescriptor itemAspectLocation = new ItemAspectLocationDescriptor();
            Mat result = new Mat();
            Image<Gray, byte> currentItemAspectImage = new Image<Gray, byte>(0, 0);

            try
            {
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

        private bool FindItemAspects()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Image<Gray, byte> currentScreenTooltipFilter = new Image<Gray, byte>(_currentScreenTooltip.Width, _currentScreenTooltip.Height, new Gray(0));
            currentScreenTooltipFilter = _currentScreenTooltip.Convert<Gray, byte>();
            currentScreenTooltipFilter = currentScreenTooltipFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

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
            ItemAspectDescriptor itemAspect = new ItemAspectDescriptor();
            Mat result = new Mat();
            Image<Gray, byte> currentItemAspectImage = new Image<Gray, byte>(0, 0);

            try
            {
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
