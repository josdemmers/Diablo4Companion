using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Collections.Concurrent;
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
        private List<ItemTooltipDescriptor> _currentTooltips = new List<ItemTooltipDescriptor>();
        Dictionary<string, Image<Gray, byte>> _imageListItemTooltips = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemTypes = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAffixes = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAspectLocations = new Dictionary<string, Image<Gray, byte>>();
        Dictionary<string, Image<Gray, byte>> _imageListItemAspects = new Dictionary<string, Image<Gray, byte>>();
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
            _imageListItemTooltips.Clear();
            _imageListItemTypes.Clear();
            _imageListItemAffixLocations.Clear();
            _imageListItemAffixes.Clear();
            _imageListItemAspectLocations.Clear();
            _imageListItemAspects.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;

            // Tooltips
            string directory = $"Images\\{systemPreset}\\Tooltips\\";
            if (Directory.Exists(directory))
            {
                string[] fileEntries = Directory.GetFiles(directory);
                foreach (string fileName in fileEntries)
                {
                    _imageListItemTooltips.TryAdd(Path.GetFileNameWithoutExtension(fileName), new Image<Gray, byte>(fileName));
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
            _imageListItemAffixLocations.TryAdd("dot-affixes_1", new Image<Gray, byte>($"Images\\{systemPreset}\\dot-affixes_1.png"));
            _imageListItemAffixLocations.TryAdd("dot-affixes_2", new Image<Gray, byte>($"Images\\{systemPreset}\\dot-affixes_2.png"));

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

            // Item aspect locations
            _imageListItemAspectLocations.TryAdd("dot-aspects_1", new Image<Gray, byte>($"Images\\{systemPreset}\\dot-aspects_1.png"));

            // Item aspects
            directory = $"Images\\{systemPreset}\\Aspects\\";
            if (Directory.Exists(directory))
            {
                string[] fileEntries = Directory.GetFiles(directory);
                foreach (string fileName in fileEntries)
                {
                    _imageListItemAspects.TryAdd(Path.GetFileNameWithoutExtension(fileName), new Image<Gray, byte>(fileName));
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
                    _currentTooltips.Clear();

                    if (_currentScreen.Height < 50)
                    {
                        _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Diablo IV window is probably minimized.");
                        return;
                    }

                    bool result = FindTooltips();
                    foreach (var tooltip in _currentTooltips)
                    {
                        result = FindItemTypes(tooltip);
                        if (result)
                        {
                            result = FindItemAffixLocations(tooltip);
                        }
                        if (result)
                        {
                            // result does not matter, always continue.
                            FindItemAffixes(tooltip);

                            result = FindItemAspectLocations(tooltip);
                        }
                        if (result)
                        {
                            // result does not matter, always continue.
                            FindItemAspects(tooltip);
                        }
                    }

                    _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Tooltip count: {_currentTooltips.Count}");

                    _eventAggregator.GetEvent<TooltipDataReadyEvent>().Publish(new TooltipDataReadyEventParams
                    {
                        Tooltips = _currentTooltips
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

            ConcurrentBag<List<ItemTooltipDescriptor>> itemTooltipBag = new ConcurrentBag<List<ItemTooltipDescriptor>>();
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

            //foreach (var itemTooltip in _imageListItemTooltips.Keys)
            //{
            //    try
            //    {
            //        itemTooltipBag.Add(FindTooltip(currentScreenFilter, itemTooltip));
            //    }
            //    catch (Exception exception)
            //    {
            //        _logger.LogError(exception, $"{MethodBase.GetCurrentMethod()?.Name}");
            //    }
            //}

            // Combine results
            var itemTooltips = new List<ItemTooltipDescriptor>();
            foreach (var itemTooltip in itemTooltipBag)
            {
                //if (itemTooltip.Any())
                //{
                //    itemTooltip.Sort((x, y) =>
                //    {
                //        return x.Similarity < y.Similarity ? -1 : x.Similarity > y.Similarity ? 1 : 0;
                //    });
                //    itemTooltips.Add(itemTooltip[0]);
                //}


                itemTooltips.AddRange(itemTooltip);
            }

            // Sort results by similarity
            itemTooltips.Sort((x, y) =>
            {
                return x.Similarity < y.Similarity ? -1 : x.Similarity > y.Similarity ? 1 : 0;
            });

            // Filter results
            var itemTooltipsFiltered = new List<ItemTooltipDescriptor>();
            foreach (var itemTooltip in itemTooltips)
            {
                if (!itemTooltipsFiltered.Any(tooltip => tooltip.Location.X >= itemTooltip.Location.X - 100 && tooltip.Location.X <= itemTooltip.Location.X + 100))
                {
                    itemTooltipsFiltered.Add(itemTooltip);
                }
            }

            foreach (var itemTooltip in itemTooltipsFiltered)
            {
                if (itemTooltip.Location.IsEmpty) continue;

                _currentTooltips.Add(new ItemTooltipDescriptor
                {
                    Location = itemTooltip.Location
                });
                
                Point[] points = new Point[]
                {
                    new Point(itemTooltip.Location.Left,itemTooltip.Location.Bottom),
                    new Point(itemTooltip.Location.Right,itemTooltip.Location.Bottom),
                    new Point(itemTooltip.Location.Right,itemTooltip.Location.Top),
                    new Point(itemTooltip.Location.Left,itemTooltip.Location.Top)
                };
                CvInvoke.Polylines(currentScreen, points, true, new MCvScalar(0, 0, 255), 5);

                if (_currentTooltips.Count >= 2) break;
            }

            //currentScreen.Save($"Logging/ScreenProcess.png");
            _eventAggregator.GetEvent<ScreenProcessItemTooltipReadyEvent>().Publish(new ScreenProcessItemTooltipReadyEventParams
            {
                ProcessedScreen = currentScreen.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return _currentTooltips.Any();
        }

        private List<ItemTooltipDescriptor> FindTooltip(Image<Gray, byte> currentScreen, string currentItemTooltip)
        {
            // Template-based Image Matching
            //double similarityThreshold = 0.005;
            //double similarityThreshold = 0.01;
            double similarityThreshold = 0.05;
            //double similarityThreshold = 0.1;

            // Initialization
            List<ItemTooltipDescriptor> tooltipList = new List<ItemTooltipDescriptor>();
            Mat result = new Mat();
            var currentItemTooltipImage = _imageListItemTooltips[currentItemTooltip].Clone();

            //currentItemTooltipImage = currentItemTooltipImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            do
            {
                CvInvoke.MatchTemplate(currentScreen, currentItemTooltipImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemTooltip}) Similarity: {String.Format("{0:0.0000000000}", minVal)} @ {minLoc.X},{minLoc.Y}");

                // Note: Ignore minVal == 0 results. Looks like they are random false positives. Requires more testing
                //if (minVal < similarityThreshold && minVal != 0)
                if (minVal < similarityThreshold)
                {
                    tooltipList.Add(new ItemTooltipDescriptor
                    {
                        Similarity = minVal,
                        Location = new Rectangle(minLoc, new Size(500, currentScreen.Height - minLoc.Y))
                    });
                }

                // Mark location so that it's only detected once.
                var rectangle = new Rectangle(minLoc, currentItemTooltipImage.Size);
                Point[] points = new Point[]
                {
                    new Point(rectangle.Left,rectangle.Bottom),
                    new Point(rectangle.Right,rectangle.Bottom),
                    new Point(rectangle.Right,rectangle.Top),
                    new Point(rectangle.Left,rectangle.Top)
                };
                CvInvoke.Polylines(currentScreen, points, true, new MCvScalar(), 5);

            } while (minVal < similarityThreshold);

            return tooltipList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item type is found.</returns>
        private bool FindItemTypes(ItemTooltipDescriptor tooltip)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();
            currentScreen.ROI = tooltip.Location;
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
                try
                {
                    itemTypeBag.Add(FindItemType(currentScreenRoiFilter, itemType));
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

                tooltip.ItemType = itemType.Name;

                Point[] points = new Point[]
                {
                    new Point(itemType.Location.Left,itemType.Location.Bottom),
                    new Point(itemType.Location.Right,itemType.Location.Bottom),
                    new Point(itemType.Location.Right,itemType.Location.Top),
                    new Point(itemType.Location.Left,itemType.Location.Top)
                };

                CvInvoke.Polylines(currentScreenRoiFilterColor, points, true, new MCvScalar(0, 0, 255), 5);

                // Skip foreach after the first valid item type is found.
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

            return !string.IsNullOrWhiteSpace(tooltip.ItemType);
        }

        private ItemTypeDescriptor FindItemType(Image<Gray, byte> currentTooltip, string currentItemType)
        {
            // Template-based Image Matching
            //double similarityThreshold = 0.005;
            //double similarityThreshold = 0.01;
            double similarityThreshold = 0.05;
            //double similarityThreshold = 0.1;

            // Initialization
            ItemTypeDescriptor itemType = new ItemTypeDescriptor { Name = currentItemType };
            Mat result = new Mat();
            var currentItemTypeImage = _imageListItemTypes[currentItemType];

            currentItemTypeImage = currentItemTypeImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            CvInvoke.MatchTemplate(currentTooltip, currentItemTypeImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);

            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemType}) Similarity: {String.Format("{0:0.00000}",minVal)}");
            if (minVal < similarityThreshold)
            {
                itemType.Similarity = minVal;
                itemType.Location = new Rectangle(minLoc, currentItemTypeImage.Size);
            }

            return itemType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item affixes are found.</returns>
        private bool FindItemAffixLocations(ItemTooltipDescriptor tooltip)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();
            currentScreen.ROI = tooltip.Location;
            var currentScreenRoi = currentScreen.Copy();
            currentScreen.ROI = Rectangle.Empty;

            // Convert the image to grayscale and apply threshold
            Image<Gray, byte> currentScreenRoiFilter = new Image<Gray, byte>(currentScreenRoi.Width, currentScreenRoi.Height, new Gray(0));
            currentScreenRoiFilter = currentScreenRoi.Convert<Gray, byte>();
            //currentScreenRoiFilter = currentScreenRoiFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            // Convert the image to color for GUI
            Image<Bgr, byte> currentScreenRoiFilterColor = new Image<Bgr, byte>(currentScreenRoiFilter.Width, currentScreenRoiFilter.Height, new Bgr());
            currentScreenRoiFilterColor = currentScreenRoiFilter.Convert<Bgr, byte>();

            ConcurrentBag<List<ItemAffixLocationDescriptor>> itemAffixLocationBag = new ConcurrentBag<List<ItemAffixLocationDescriptor>>();
            Parallel.ForEach(_imageListItemAffixLocations.Keys, itemAffixLocation =>
            {
                try
                {
                    itemAffixLocationBag.Add(FindItemAffixLocation(currentScreenRoiFilter, Path.GetFileNameWithoutExtension(itemAffixLocation)));
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
                tooltip.ItemAffixLocations.Add(itemAffixLocation.Location);

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
            //double similarityThreshold = 0.005;
            //double similarityThreshold = 0.01;
            double similarityThreshold = 0.05;
            //double similarityThreshold = 0.1;

            // Initialization
            List<ItemAffixLocationDescriptor> itemAffixLocations = new List<ItemAffixLocationDescriptor>();
            Mat result = new Mat();
            var currentItemAffixLocationImage = _imageListItemAffixLocations[currentItemAffixLocation].Clone();
            //currentItemAffixLocationImage = currentItemAffixLocationImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MatchTemplate(currentTooltip, currentItemAffixLocationImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
            Mat resultNorm = new Mat();
            CvInvoke.Normalize(result, resultNorm, 0, 1, Emgu.CV.CvEnum.NormType.MinMax, Emgu.CV.CvEnum.DepthType.Cv64F);
            Matrix<double> matches = new Matrix<double>(resultNorm.Size);
            resultNorm.CopyTo(matches);

            do
            {
                CvInvoke.MinMaxLoc(matches, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAffixLocation}) Similarity: {String.Format("{0:0.00000}", minVal)}");

                // Too many similarities. Need to add some constraints to filter out false positives.
                //if (minLoc.X < currentTooltip.Width / 5 && minVal < similarityThreshold)
                if (minLoc.X < 60 && minVal < similarityThreshold)
                {
                    itemAffixLocations.Add(new ItemAffixLocationDescriptor
                    {
                        Similarity = minVal,
                        Location = new Rectangle(minLoc, currentItemAffixLocationImage.Size)
                    });

                    matches[minLoc.Y, minLoc.X] = 0.5;
                    matches[maxLoc.Y, maxLoc.X] = 0.5;

                    // Mark location so that it's only detected once.
                    Point[] points = new Point[]
                    {
                        new Point(itemAffixLocations.Last().Location.Left,itemAffixLocations.Last().Location.Bottom),
                        new Point(itemAffixLocations.Last().Location.Right,itemAffixLocations.Last().Location.Bottom),
                        new Point(itemAffixLocations.Last().Location.Right,itemAffixLocations.Last().Location.Top),
                        new Point(itemAffixLocations.Last().Location.Left,itemAffixLocations.Last().Location.Top)
                    };
                    CvInvoke.Polylines(currentTooltip, points, true, new MCvScalar(), 5);
                }
                else
                {
                    matches[minLoc.Y, minLoc.X] = 0.5;
                    matches[maxLoc.Y, maxLoc.X] = 0.5;
                }
            } while (minVal < similarityThreshold);

            return itemAffixLocations;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item affixes are found.</returns>
        private bool FindItemAffixes(ItemTooltipDescriptor tooltip)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();
            currentScreen.ROI = tooltip.Location;
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
            var itemAffixesPerType = itemAffixes?.FindAll(itemAffix => tooltip.ItemType.StartsWith($"{itemAffix.Type}_"));
            if (itemAffixesPerType != null) 
            {
                ConcurrentBag<List<ItemAffixDescriptor>> itemAffixBag = new ConcurrentBag<List<ItemAffixDescriptor>>();
                Parallel.ForEach(itemAffixesPerType, itemAffix =>
                {
                    try
                    {
                        itemAffixBag.Add(FindItemAffix(currentScreenRoiFilter, Path.GetFileNameWithoutExtension(itemAffix.FileName)));
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

                    tooltip.ItemAffixes.Add(itemAffix.Location);

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

            return tooltip.ItemAffixes.Any();
        }

        private List<ItemAffixDescriptor> FindItemAffix(Image<Gray, byte> currentTooltip, string currentItemAffix)
        {
            // Template-based Image Matching
            //double similarityThreshold = 0.005;
            double similarityThreshold = 0.01;
            //double similarityThreshold = 0.05;
            //double similarityThreshold = 0.1;

            // Initialization
            List<ItemAffixDescriptor> itemAffixes = new List<ItemAffixDescriptor>();
            Mat result = new Mat();
            var currentItemAffixImage = _imageListItemAffixes[currentItemAffix].Clone();

            currentItemAffixImage = currentItemAffixImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));

            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MatchTemplate(currentTooltip, currentItemAffixImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
            
            do
            {
                CvInvoke.MatchTemplate(currentTooltip, currentItemAffixImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAffix}) Similarity: {String.Format("{0:0.00000}", minVal)}");

                // Note: Ignore minVal == 0 results. Looks like they are random false positives. Requires more testing
                //if (minVal < similarityThreshold && minVal != 0)
                if (minVal < similarityThreshold)
                {
                    itemAffixes.Add(new ItemAffixDescriptor
                    {
                        Similarity = minVal,
                        Location = new Rectangle(minLoc, currentItemAffixImage.Size)
                    });
                }

                // Mark location so that it's only detected once.
                var rectangle = new Rectangle(minLoc, currentItemAffixImage.Size);
                Point[] points = new Point[]
                {
                    new Point(rectangle.Left,rectangle.Bottom),
                    new Point(rectangle.Right,rectangle.Bottom),
                    new Point(rectangle.Right,rectangle.Top),
                    new Point(rectangle.Left,rectangle.Top)
                };
                CvInvoke.Polylines(currentTooltip, points, true, new MCvScalar(), 5);

            } while (minVal < similarityThreshold);

            return itemAffixes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item aspect is found.</returns>
        private bool FindItemAspectLocations(ItemTooltipDescriptor tooltip)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();
            currentScreen.ROI = tooltip.Location;
            var currentScreenRoi = currentScreen.Copy();
            currentScreen.ROI = Rectangle.Empty;

            // Convert the image to grayscale and apply threshold
            Image<Gray, byte> currentScreenRoiFilter = new Image<Gray, byte>(currentScreenRoi.Width, currentScreenRoi.Height, new Gray(0));
            currentScreenRoiFilter = currentScreenRoi.Convert<Gray, byte>();
            //currentScreenRoiFilter = currentScreenRoiFilter.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            // Convert the image to color for GUI
            Image<Bgr, byte> currentScreenRoiFilterColor = new Image<Bgr, byte>(currentScreenRoiFilter.Width, currentScreenRoiFilter.Height, new Bgr());
            currentScreenRoiFilterColor = currentScreenRoiFilter.Convert<Bgr, byte>();

            ConcurrentBag<ItemAspectLocationDescriptor> itemAspectLocationBag = new ConcurrentBag<ItemAspectLocationDescriptor>();
            Parallel.ForEach(_imageListItemAspectLocations.Keys, itemAspectLocation =>
            {
                try
                {
                    itemAspectLocationBag.Add(FindItemAspectLocation(currentScreenRoiFilter, Path.GetFileNameWithoutExtension(itemAspectLocation)));
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

                tooltip.ItemAspectLocation = itemAspectLocation.Location;

                Point[] points = new Point[]
                {
                    new Point(itemAspectLocation.Location.Left,itemAspectLocation.Location.Bottom),
                    new Point(itemAspectLocation.Location.Right,itemAspectLocation.Location.Bottom),
                    new Point(itemAspectLocation.Location.Right,itemAspectLocation.Location.Top),
                    new Point(itemAspectLocation.Location.Left,itemAspectLocation.Location.Top)
                };

                CvInvoke.Polylines(currentScreenRoiFilterColor, points, true, new MCvScalar(0, 0, 255), 5);

                break;
            }

            //currentScreenRoiFilterColor.Save($"Logging/ScreenProcess.png");
            _eventAggregator.GetEvent<ScreenProcessItemAspectLocationReadyEvent>().Publish(new ScreenProcessItemAspectLocationReadyEventParams
            {
                ProcessedScreen = currentScreenRoiFilterColor.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !tooltip.ItemAspectLocation.IsEmpty;
        }

        private ItemAspectLocationDescriptor FindItemAspectLocation(Image<Gray, byte> currentTooltip, string currentItemAspectLocation)
        {
            // Template-based Image Matching
            //double similarityThreshold = 0.005;
            //double similarityThreshold = 0.01;
            double similarityThreshold = 0.05;
            //double similarityThreshold = 0.1;

            // Initialization
            ItemAspectLocationDescriptor itemAspectLocation = new ItemAspectLocationDescriptor();
            Mat result = new Mat();
            var currentItemAspectImage = _imageListItemAspectLocations[currentItemAspectLocation].Clone();
            CvInvoke.MatchTemplate(currentTooltip, currentItemAspectImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);

            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemType}) Similarity: {String.Format("{0:0.00000}",minVal)}");
            if (minVal < similarityThreshold)
            {
                itemAspectLocation.Similarity = minVal;
                itemAspectLocation.Location = new Rectangle(minLoc, currentItemAspectImage.Size);
            }

            return itemAspectLocation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when item aspect is found.</returns>
        private bool FindItemAspects(ItemTooltipDescriptor tooltip)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var currentScreen = _currentScreen.Clone();
            currentScreen.ROI = tooltip.Location;
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
            var itemAspects = _affixPresetManager.AffixPresets.FirstOrDefault(s => s.Name == affixPreset)?.ItemAspects;
            var itemAspectsPerType = itemAspects?.FindAll(itemAspect => tooltip.ItemType.StartsWith($"{itemAspect.Type}_"));
            if (itemAspectsPerType != null)
            {
                ConcurrentBag<ItemAspectDescriptor> itemAspectBag = new ConcurrentBag<ItemAspectDescriptor>();
                Parallel.ForEach(itemAspectsPerType, itemAspect =>
                {
                    try
                    {
                        itemAspectBag.Add(FindItemAspect(currentScreenRoiFilter, Path.GetFileNameWithoutExtension(itemAspect.FileName)));
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

                    tooltip.ItemAspect = itemAspect.Location;

                    Point[] points = new Point[]
                    {
                        new Point(itemAspect.Location.Left,itemAspect.Location.Bottom),
                        new Point(itemAspect.Location.Right,itemAspect.Location.Bottom),
                        new Point(itemAspect.Location.Right,itemAspect.Location.Top),
                        new Point(itemAspect.Location.Left,itemAspect.Location.Top)
                    };

                    CvInvoke.Polylines(currentScreenRoiFilterColor, points, true, new MCvScalar(0, 0, 255), 5);

                    break;
                }
            }

            //currentScreenRoiFilterColor.Save($"Logging/ScreenProcess.png");
            _eventAggregator.GetEvent<ScreenProcessItemAspectReadyEvent>().Publish(new ScreenProcessItemAspectReadyEventParams
            {
                ProcessedScreen = currentScreenRoiFilterColor.ToBitmap()
            });

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: Elapsed time: {elapsedMs}");

            return !tooltip.ItemAspect.IsEmpty;
        }

        private ItemAspectDescriptor FindItemAspect(Image<Gray, byte> currentTooltip, string currentItemAspect)
        {
            // Template-based Image Matching
            //double similarityThreshold = 0.005;
            //double similarityThreshold = 0.01;
            double similarityThreshold = 0.05;
            //double similarityThreshold = 0.1;

            // Initialization
            ItemAspectDescriptor itemAspect = new ItemAspectDescriptor();
            Mat result = new Mat();
            var currentItemAspectImage = _imageListItemAspects[currentItemAspect].Clone();

            currentItemAspectImage = currentItemAspectImage.ThresholdBinaryInv(new Gray(_settingsManager.Settings.ThresholdMin), new Gray(_settingsManager.Settings.ThresholdMax));
            CvInvoke.MatchTemplate(currentTooltip, currentItemAspectImage, result, Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed);

            double minVal = 0.0;
            double maxVal = 0.0;
            Point minLoc = new Point();
            Point maxLoc = new Point();

            CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);
            //_logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: ({currentItemAspect}) Similarity: {String.Format("{0:0.00000}", minVal)}");
            if (minVal < similarityThreshold)
            {
                itemAspect.Similarity = minVal;
                itemAspect.Location = new Rectangle(minLoc, currentItemAspectImage.Size);
            }

            return itemAspect;
        }

        #endregion
    }
}
