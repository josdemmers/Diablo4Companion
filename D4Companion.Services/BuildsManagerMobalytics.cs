using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using D4Companion.Messages;
using FuzzierSharp;
using FuzzierSharp.SimilarityRatio;
using FuzzierSharp.SimilarityRatio.Scorer.Composite;
using FuzzierSharp.SimilarityRatio.Scorer.StrategySensitive;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using System.Xml.Linq;

namespace D4Companion.Services
{
    public class BuildsManagerMobalytics : IBuildsManagerMobalytics
    {
        private readonly IAffixManager _affixManager;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private List<AffixInfo> _affixes = new List<AffixInfo>();
        private List<string> _affixDescriptions = new List<string>();
        private Dictionary<string, string> _affixMapDescriptionToId = new Dictionary<string, string>();
        private List<AspectInfo> _aspects = new List<AspectInfo>();
        private List<string> _aspectNames = new List<string>();
        private Dictionary<string, string> _aspectMapNameToId = new Dictionary<string, string>();
        private string _buildUrl = string.Empty;
        private object _lockTimerTimeout = new();
        private List<MobalyticsBuild> _mobalyticsBuilds = new();
        private List<MobalyticsProfile> _mobalyticsProfiles = new();
        private List<RuneInfo> _runes = new List<RuneInfo>();
        private List<string> _runeNames = new List<string>();
        private Dictionary<string, string> _runeMapNameToId = new Dictionary<string, string>();
        private System.Timers.Timer _timerTimeout = new();
        private List<UniqueInfo> _uniques = new List<UniqueInfo>();
        private List<string> _uniqueNames = new List<string>();
        private Dictionary<string, string> _uniqueMapNameToId = new Dictionary<string, string>();
        private ChromeDriver? _webDriver = null;
        private DevToolsSession? _devToolsSession = null;
        private WebDriverWait? _webDriverWait = null;
        private int _webDriverProcessId = 0;

        // Start of Constructors region

        #region Constructors

        public BuildsManagerMobalytics(ILogger<BuildsManagerMobalytics> logger, IAffixManager affixManager, ISettingsManager settingsManager)
        {
            // Init services
            _affixManager = affixManager;
            _logger = logger;
            _settingsManager = settingsManager;

            // Init data
            InitAffixData();
            InitAspectData();
            InitRuneData();
            InitUniqueData();

            // Init timers
            _timerTimeout.Interval = 10000;
            _timerTimeout.Elapsed += TimerTimeoutElapsedHandler;

            // Load available Mobalytics builds and profiles.
            Task.Factory.StartNew(() =>
            {
                LoadAvailableMobalyticsBuilds();
                LoadAvailableMobalyticsProfiles();
            });
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<MobalyticsBuild> MobalyticsBuilds { get => _mobalyticsBuilds; set => _mobalyticsBuilds = value; }
        public List<MobalyticsProfile> MobalyticsProfiles { get => _mobalyticsProfiles; set => _mobalyticsProfiles = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void TimerTimeoutElapsedHandler(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _timerTimeout.Stop();

            WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
            {
                Status = $"Timeout occurred."
            }));

            FinalizeBuildDownload();
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitAffixData()
        {
            _affixes.Clear();
            string resourcePath = @".\Data\Affixes.enUS.json";
            using (FileStream? stream = File.OpenRead(resourcePath))
            {
                if (stream != null)
                {
                    // create the options
                    var options = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    // register the converter
                    options.Converters.Add(new BoolConverter());
                    options.Converters.Add(new IntConverter());

                    _affixes = JsonSerializer.Deserialize<List<AffixInfo>>(stream, options) ?? new List<AffixInfo>();
                }
            }

            // Create affix description list for FuzzierSharp
            _affixDescriptions.Clear();
            _affixDescriptions = _affixes.Select(affix =>
            {
                // Remove class restrictions from description. Mobalytics does not show this information.
                return affix.DescriptionClean.Contains(")") ? affix.DescriptionClean.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries)[0] : affix.DescriptionClean;
            }).ToList();

            // Create dictionary to map affix description with affix id
            _affixMapDescriptionToId.Clear();
            _affixMapDescriptionToId = _affixes.ToDictionary(affix =>
            {
                // Remove class restrictions from description. Mobalytics does not show this information.
                return affix.DescriptionClean.Contains(")") ? affix.DescriptionClean.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries)[0] : affix.DescriptionClean;
            }, affix => affix.IdName);
        }

        private void InitAspectData()
        {
            _aspects.Clear();
            string resourcePath = @".\Data\Aspects.enUS.json";
            using (FileStream? stream = File.OpenRead(resourcePath))
            {
                if (stream != null)
                {
                    // create the options
                    var options = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    // register the converter
                    options.Converters.Add(new BoolConverter());
                    options.Converters.Add(new IntConverter());

                    _aspects = JsonSerializer.Deserialize<List<AspectInfo>>(stream, options) ?? new List<AspectInfo>();
                }
            }

            // Create aspect name list for FuzzierSharp
            _aspectNames.Clear();
            _aspectNames = _aspects.Select(aspect => aspect.Name).ToList();

            // Create dictionary to map aspect name with aspect id
            _aspectMapNameToId.Clear();
            _aspectMapNameToId = _aspects.ToDictionary(aspect => aspect.Name, aspect => aspect.IdName);
        }

        private void InitDevTools()
        {
            if (_webDriver == null) return;

            try
            {
                _devToolsSession = _webDriver.GetDevToolsSession();
            }
            catch (Exception exception)
            {
                WeakReferenceMessenger.Default.Send(new ExceptionOccurredMessage(new ExceptionOccurredMessageParams
                {
                    Message = $"Chrome out-of-date. Exception: {exception?.InnerException?.Message ?? "null"}"
                }));
                return;
            }

            // Tweak settings when handling bigger json responses
            var enableCommandSettingsType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "EnableCommandSettings");
            if (enableCommandSettingsType == null) throw new Exception("DevTools initialization failed.");
            var enableCommandSettings = Activator.CreateInstance(enableCommandSettingsType);
            //enableCommandSettingsType.GetProperty("MaxPostDataSize")?.SetValue(enableCommandSettings, (long?)(20 * 1024 * 1024));       // 20 MB post data
            //enableCommandSettingsType.GetProperty("MaxResourceBufferSize")?.SetValue(enableCommandSettings, (long?)(20 * 1024 * 1024)); // 20 MB per resource
            //enableCommandSettingsType.GetProperty("MaxTotalBufferSize")?.SetValue(enableCommandSettings, (long?)(200 * 1024 * 1024));   // 200 MB total buffer

            var setCacheDisabledCommandSettingsType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "SetCacheDisabledCommandSettings");
            if (setCacheDisabledCommandSettingsType == null) throw new Exception("DevTools initialization failed.");
            var setCacheDisabledCommandSettings = Activator.CreateInstance(setCacheDisabledCommandSettingsType);
            setCacheDisabledCommandSettingsType.GetProperty("CacheDisabled")?.SetValue(setCacheDisabledCommandSettings, true);

            var clearBrowserCacheCommandSettingsType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "ClearBrowserCacheCommandSettings");
            if (clearBrowserCacheCommandSettingsType == null) throw new Exception("DevTools initialization failed.");
            var clearBrowserCacheCommandSettings = Activator.CreateInstance(clearBrowserCacheCommandSettingsType);
            var clearBrowserCookiesCommandSettingsType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "ClearBrowserCookiesCommandSettings");
            if (clearBrowserCookiesCommandSettingsType == null) throw new Exception("DevTools initialization failed.");
            var clearBrowserCookiesCommandSettings = Activator.CreateInstance(clearBrowserCookiesCommandSettingsType);

            var networkAdapterType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "NetworkAdapter");
            if (networkAdapterType == null) throw new Exception("DevTools initialization failed.");
            var networkAdapter = Activator.CreateInstance(networkAdapterType, _devToolsSession);
            var enableMethod = networkAdapterType.GetMethod("Enable");
            var clearBrowserCacheMethod = networkAdapterType.GetMethod("ClearBrowserCache");
            var clearBrowserCookiesMethod = networkAdapterType.GetMethod("ClearBrowserCookies");
            var setCacheDisabledMethod = networkAdapterType.GetMethod("SetCacheDisabled");
            enableMethod?.Invoke(networkAdapter, new[] { enableCommandSettings, CancellationToken.None, null, true });
            clearBrowserCacheMethod?.Invoke(networkAdapter, new[] { clearBrowserCacheCommandSettings, CancellationToken.None, null, true });
            clearBrowserCookiesMethod?.Invoke(networkAdapter, new[] { clearBrowserCookiesCommandSettings, CancellationToken.None, null, true });
            setCacheDisabledMethod?.Invoke(networkAdapter, new[] { setCacheDisabledCommandSettings, CancellationToken.None, null, true });

            // Create event handler
            var responseReceivedEvent = networkAdapterType.GetEvent("ResponseReceived");
            if (responseReceivedEvent != null)
            {
                // Get the delegate type for the event
                var eventHandlerType = responseReceivedEvent.EventHandlerType;

                // Create a dynamic handler using a lambda
                var handler = (EventHandler)((sender, e) =>
                {
                    try
                    {
                        lock (_lockTimerTimeout)
                        {
                            // Reset timeout timer
                            _timerTimeout.Stop();
                            _timerTimeout.Start();
                        }

                        // Use dynamic since we don’t know the exact type
                        dynamic args = e;
                                          
                        //System.Diagnostics.Debug.WriteLine($"ResponseReceived: requestId={args.RequestId}, url={args.Response.Url}");
                        if (args.Response.MimeType.Equals("application/json") && args.Response.Url.Contains("api/diablo4"))
                        {
                            // Give some time for the response body to be ready.
                            Thread.Sleep(1000);

                            // GetResponseBody method
                            var getResponseBodyMethod = networkAdapterType.GetMethod("GetResponseBody");
                            var getResponseBodyCommandSettingsType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "GetResponseBodyCommandSettings");
                            if (getResponseBodyCommandSettingsType == null) throw new Exception("DevTools initialization failed.");
                            var getResponseBodyCommandSettings = Activator.CreateInstance(getResponseBodyCommandSettingsType);
                            getResponseBodyCommandSettingsType.GetProperty("RequestId")?.SetValue(getResponseBodyCommandSettings, args.RequestId);
                            // Call GetResponseBody
                            var task = (Task?)getResponseBodyMethod?.Invoke(networkAdapter, new[] { getResponseBodyCommandSettings, CancellationToken.None, null, true });
                            task?.Wait();
                            var resultProperty = task?.GetType().GetProperty("Result");
                            dynamic? body = resultProperty?.GetValue(task);

                            string json = body?.Body ?? string.Empty;
                            //System.Diagnostics.Debug.WriteLine($"Response body for {args.Response.Url}: {json.Substring(0,100)}");
                            if (json.StartsWith("{\"data\":{\"game\":{\"documents\":{\"userGeneratedDocumentById\":"))
                            {
                                ParseJsonBuild(json);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore exceptions in event handler
                        // Failed processes will be handled by the timeout timer.
                    }
                });

                // Convert the lambda to the correct delegate type
                var delegateHandler = Delegate.CreateDelegate(eventHandlerType!, handler.Target, handler.Method);

                // Attach handler
                responseReceivedEvent.AddEventHandler(networkAdapter, delegateHandler);
            }
        }

        private void InitRuneData()
        {
            _runes.Clear();
            string resourcePath = @".\Data\Runes.enUS.json";
            using (FileStream? stream = File.OpenRead(resourcePath))
            {
                if (stream != null)
                {
                    // create the options
                    var options = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    // register the converter
                    options.Converters.Add(new BoolConverter());
                    options.Converters.Add(new IntConverter());

                    _runes = JsonSerializer.Deserialize<List<RuneInfo>>(stream, options) ?? new List<RuneInfo>();
                }
            }

            // Create rune name list for FuzzierSharp
            _runeNames.Clear();
            _runeNames = _runes.Select(rune => rune.Name).ToList();

            // Create dictionary to map rune name with run id
            _runeMapNameToId.Clear();
            _runeMapNameToId = _runes.ToDictionary(rune => rune.Name, rune => rune.IdName);
        }

        private void InitUniqueData()
        {
            _uniques.Clear();
            string resourcePath = @".\Data\Uniques.enUS.json";
            using (FileStream? stream = File.OpenRead(resourcePath))
            {
                if (stream != null)
                {
                    // create the options
                    var options = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    // register the converter
                    options.Converters.Add(new BoolConverter());
                    options.Converters.Add(new IntConverter());

                    _uniques = JsonSerializer.Deserialize<List<UniqueInfo>>(stream, options) ?? new List<UniqueInfo>();
                }
            }

            // Create unique name list for FuzzierSharp
            _uniqueNames.Clear();
            _uniqueNames = _uniques.Select(unique => unique.Name).ToList();

            // Create dictionary to map unique name with unique id
            _uniqueMapNameToId.Clear();
            //_uniqueMapNameToId = _uniques.ToDictionary(unique => unique.Name, unique => unique.IdName);
            foreach (var unique in _uniques)
            {
                if (!_uniqueMapNameToId.ContainsKey(unique.Name))
                {
                    _uniqueMapNameToId.Add(unique.Name, unique.IdName);
                }
            }
        }

        private void InitSelenium()
        {
            // Options: Headless, size, security, ...
            var options = new ChromeOptions();

            if(!_settingsManager.Settings.IsShowBrowserMobalyticsEnabled)
            {
                // Note: ChromeDriver 129 is bugged and causes blank window when using headless mode. Test again with the release of 130.
                //options.AddArgument("--headless=old"); //v129 and older
                options.AddArgument("--headless"); // v130+

                // Note: ChromeDriver DevToolsActivePort file doesn't exist exceptions. Below fix might be needed in combination with "--headless=old"
                // https://issues.chromium.org/issues/42323434#comment36
                //options.AddArgument("--remote-debugging-pipe");
            }

            options.AddArgument("--disable-gpu"); // Applicable to windows os only

            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-popup-blocking");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--dns-prefetch-disable");
            options.AddArgument("--disable-dev-shm-usage"); // Overcome limited resource problems
            options.AddArgument("--no-sandbox"); // Bypass OS security model
            options.AddArgument("--window-size=1600,900");
            options.AddArgument("--window-position=-32000,-32000");

            // Cache related settings
            options.AddArgument("--disable-cache");
            options.AddArgument("--disk-cache-size=0");
            options.AddArgument("--media-cache-size=0");

            options.AddArgument("--user-agent=Diablo4Companion/1.0");

            // Service
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            // Create driver
            _webDriver = new ChromeDriver(service: service, options: options);
            _webDriverWait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10));
            _webDriverProcessId = service.ProcessId;

            // Init DevTools
            InitDevTools();
        }        

        public void CreatePresetFromMobalyticsBuild(MobalyticsBuildVariant mobalyticsBuild, string buildNameOriginal, string buildName)
        {
            buildName = string.IsNullOrWhiteSpace(buildName) ? buildNameOriginal : buildName;

            // Note: Only allow one Mobalytics build. Update if already exists.
            _affixManager.AffixPresets.RemoveAll(p => p.Name.Equals(buildName));

            var affixPreset = mobalyticsBuild.AffixPreset.Clone();
            affixPreset.Name = buildName;

            _affixManager.AddAffixPreset(affixPreset);
        }

        public void DownloadMobalyticsBuild(string buildUrl)
        {
            try
            {
                WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                {
                    Status = $"Preparing browser instance."
                }));

                buildUrl = buildUrl.ToLower();
                _buildUrl = buildUrl;

                if (_webDriver == null) InitSelenium();
                if (_webDriver == null) throw new Exception("WebDriver initialization failed.");
                if (_webDriverWait == null) throw new Exception("WebDriverWait initialization failed.");
                if (_devToolsSession == null) throw new Exception("DevToolsSession initialization failed.");

                WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                {
                    Status = $"Downloading {buildUrl}."
                }));
                _webDriver.Navigate().GoToUrl(buildUrl);
                
                if (buildUrl.Contains("/profile/") && !buildUrl.Contains("/builds/"))
                {
                    // For profile page use javascript to extract data.

                    // Wait until all required resources are loaded
                    var result = _webDriverWait.Until(driver =>
                    {
                        var js = (IJavaScriptExecutor)driver;
                        return js.ExecuteScript("return typeof window.__PRELOADED_STATE__ !== 'undefined';");
                    });

                    if (result != null)
                    {
                        var js = (IJavaScriptExecutor)_webDriver;
                        Dictionary<string, object>? jsonDictionary = js.ExecuteScript("return window.__PRELOADED_STATE__;") as Dictionary<string, object>;

                        if (jsonDictionary != null && jsonDictionary.ContainsKey("diablo4State"))
                        {
                            string jsonString = JsonSerializer.Serialize(jsonDictionary["diablo4State"]);
                            ParseJsonProfile(buildUrl, jsonString);
                        }
                    }
                }                
                else if (HasBuildUrlSlugFormat(buildUrl))
                {
                    // For build page with slug name instead of id use javascript to extract data.

                    // Wait until all required resources are loaded
                    var result = _webDriverWait.Until(driver =>
                    {
                        var js = (IJavaScriptExecutor)driver;
                        return js.ExecuteScript("return typeof window.__PRELOADED_STATE__ !== 'undefined';");
                    });

                    if (result != null)
                    {
                        var js = (IJavaScriptExecutor)_webDriver;
                        Dictionary<string, object>? jsonDictionary = js.ExecuteScript("return window.__PRELOADED_STATE__;") as Dictionary<string, object>;

                        if (jsonDictionary != null && jsonDictionary.ContainsKey("diablo4State"))
                        {
                            string jsonString = JsonSerializer.Serialize(jsonDictionary["diablo4State"]);
                            ParseJsonBuildBySlug(jsonString);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{MethodBase.GetCurrentMethod()?.Name} ({buildUrl})");

                WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                {
                    Message = $"Failed to download from Mobalytics ({buildUrl})"
                }));

                WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                {
                    Status = $"Failed. See log."
                }));

                FinalizeBuildDownload();
            }
        }

        private void ConvertBuildVariants(MobalyticsBuild mobalyticsBuild)
        {
            foreach (var variant in mobalyticsBuild.Variants)
            {
                WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                {
                    Build = mobalyticsBuild,
                    Status = $"Converting {variant.Name}."
                }));

                var affixPreset = new AffixPreset
                {
                    Name = variant.Name
                };

                // Prepare affixes
                List<Tuple<string, MobalyticsAffix>> affixesMobalytics = new List<Tuple<string, MobalyticsAffix>>();

                foreach (var affixMobalytics in variant.Helm)
                {
                    affixesMobalytics.Add(new Tuple<string, MobalyticsAffix>(Constants.ItemTypeConstants.Helm, affixMobalytics));
                }
                foreach (var affixMobalytics in variant.Chest)
                {
                    affixesMobalytics.Add(new Tuple<string, MobalyticsAffix>(Constants.ItemTypeConstants.Chest, affixMobalytics));
                }
                foreach (var affixMobalytics in variant.Gloves)
                {
                    affixesMobalytics.Add(new Tuple<string, MobalyticsAffix>(Constants.ItemTypeConstants.Gloves, affixMobalytics));
                }
                foreach (var affixMobalytics in variant.Pants)
                {
                    affixesMobalytics.Add(new Tuple<string, MobalyticsAffix>(Constants.ItemTypeConstants.Pants, affixMobalytics));
                }
                foreach (var affixMobalytics in variant.Boots)
                {
                    affixesMobalytics.Add(new Tuple<string, MobalyticsAffix>(Constants.ItemTypeConstants.Boots, affixMobalytics));
                }
                foreach (var affixMobalytics in variant.Amulet)
                {
                    affixesMobalytics.Add(new Tuple<string, MobalyticsAffix>(Constants.ItemTypeConstants.Amulet, affixMobalytics));
                }
                foreach (var affixMobalytics in variant.Ring)
                {
                    affixesMobalytics.Add(new Tuple<string, MobalyticsAffix>(Constants.ItemTypeConstants.Ring, affixMobalytics));
                }
                foreach (var affixMobalytics in variant.Weapon)
                {
                    affixesMobalytics.Add(new Tuple<string, MobalyticsAffix>(Constants.ItemTypeConstants.Weapon, affixMobalytics));
                }
                foreach (var affixMobalytics in variant.Ranged)
                {
                    affixesMobalytics.Add(new Tuple<string, MobalyticsAffix>(Constants.ItemTypeConstants.Ranged, affixMobalytics));
                }
                foreach (var affixMobalytics in variant.Offhand)
                {
                    affixesMobalytics.Add(new Tuple<string, MobalyticsAffix>(Constants.ItemTypeConstants.Offhand, affixMobalytics));
                }

                // Find matching affix ids
                ConcurrentBag<ItemAffix> itemAffixBag = new ConcurrentBag<ItemAffix>();
                Parallel.ForEach(affixesMobalytics, affixMobalytics =>
                {
                    var itemAffixResult = ConvertItemAffix(affixMobalytics);
                    itemAffixBag.Add(itemAffixResult);
                });
                affixPreset.ItemAffixes.AddRange(itemAffixBag);

                // Sort affixes
                affixPreset.ItemAffixes.Sort((x, y) =>
                {
                    if (x.Id == y.Id && x.IsImplicit == y.IsImplicit && x.IsTempered == y.IsTempered) return 0;

                    int result = x.IsTempered && !y.IsTempered ? 1 : y.IsTempered && !x.IsTempered ? -1 : 0;
                    if (result == 0)
                    {
                        result = x.IsImplicit && !y.IsImplicit ? -1 : y.IsImplicit && !x.IsImplicit ? 1 : 0;
                    }

                    return result;
                });

                // Remove duplicates
                affixPreset.ItemAffixes = affixPreset.ItemAffixes.DistinctBy(a => new { a.Id, a.Type, a.IsImplicit, a.IsTempered }).ToList();

                // Find matching aspect ids
                ConcurrentBag<ItemAffix> itemAspectBag = new ConcurrentBag<ItemAffix>();
                Parallel.ForEach(variant.Aspect, aspect =>
                {
                    var itemAspectResult = ConvertItemAspect(aspect);
                    itemAspectBag.Add(itemAspectResult);
                });
                foreach (var aspect in itemAspectBag)
                {
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Helm });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Chest });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Gloves });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Pants });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Boots });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Amulet });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Ring });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Weapon });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Ranged });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Offhand });
                }

                // Find matching rune ids
                ConcurrentBag<ItemAffix> itemRuneBag = new ConcurrentBag<ItemAffix>();
                Parallel.ForEach(variant.Runes, rune =>
                {
                    var itemRuneResult = ConvertItemRune(rune);
                    itemRuneBag.Add(itemRuneResult);
                });
                foreach (var rune in itemRuneBag)
                {
                    affixPreset.ItemRunes.Add(new ItemAffix { Id = rune.Id, Type = Constants.ItemTypeConstants.Rune });
                }

                // Find matching unique ids
                ConcurrentBag<ItemAffix> itemUniqueBag = new ConcurrentBag<ItemAffix>();
                Parallel.ForEach(variant.Uniques, unique =>
                {
                    var itemUniqueResult = ConvertItemUnique(unique);
                    itemUniqueBag.Add(itemUniqueResult);
                });
                affixPreset.ItemUniques.AddRange(itemUniqueBag);

                // Add paragon board
                affixPreset.ParagonBoardsList.Add(variant.ParagonBoards);

                variant.AffixPreset = affixPreset;
                WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                {
                    Build = mobalyticsBuild,
                    Status = $"Converted {variant.Name}."
                }));
            }
        }

        private ItemAffix ConvertItemAffix(Tuple<string, MobalyticsAffix> affixDescription)
        {
            string affixIdName = string.Empty;
            string itemType = affixDescription.Item1;
            MobalyticsAffix mobalyticsAffix = affixDescription.Item2;

            string affix = string.Empty;
            var results = new List<(string affix, string affixMatch, int score)>();
            for (int i = 0; i < mobalyticsAffix.AffixTextList.Count; i++)
            {
                affix = string.Join(" ", mobalyticsAffix.AffixTextList.Skip(mobalyticsAffix.AffixTextList.Count - (i + 1)).Take(i + 1));
                var fuzzyMatch = Process.ExtractOne(affix, _affixDescriptions, scorer: ScorerCache.Get<DefaultRatioScorer>());
                results.Add((affix, fuzzyMatch.Value, fuzzyMatch.Score));
            }

            var result = results
                .OrderByDescending(r => r.score)
                .ThenByDescending(r => r.affix.Length)
                .First();

            affixIdName = _affixMapDescriptionToId[result.affixMatch];

            Color color = mobalyticsAffix.IsImplicit ? _settingsManager.Settings.DefaultColorImplicit :
                mobalyticsAffix.IsGreater ? _settingsManager.Settings.DefaultColorGreater :
                mobalyticsAffix.IsTempered ? _settingsManager.Settings.DefaultColorTempered :
                _settingsManager.Settings.DefaultColorNormal;
            return new ItemAffix
            {
                Id = affixIdName,
                Type = itemType,
                Color = color,
                IsGreater = mobalyticsAffix.IsGreater,
                IsImplicit = mobalyticsAffix.IsImplicit,
                IsTempered = mobalyticsAffix.IsTempered,
                TuningPrisms = _affixManager.GetAffixTuningPrismsByIdName(affixIdName).ToList()
            };
        }

        private ItemAffix ConvertItemAspect(string aspect)
        {
            string aspectId = string.Empty;

            var result = Process.ExtractOne(aspect.Replace("Aspect", string.Empty, StringComparison.OrdinalIgnoreCase), _aspectNames, scorer: ScorerCache.Get<WeightedRatioScorer>());
            aspectId = _aspectMapNameToId[result.Value];

            return new ItemAffix
            {
                Id = aspectId,
                Type = Constants.ItemTypeConstants.Helm,
                Color = _settingsManager.Settings.DefaultColorAspects
            };
        }

        private ItemAffix ConvertItemRune(string rune)
        {
            string runeId = string.Empty;

            var result = Process.ExtractOne(rune, _runeNames, scorer: ScorerCache.Get<WeightedRatioScorer>());
            runeId = _runeMapNameToId[result.Value];

            return new ItemAffix
            {
                Id = runeId,
                Type = Constants.ItemTypeConstants.Rune,
                Color = _settingsManager.Settings.DefaultColorRunes
            };
        }

        private ItemAffix ConvertItemUnique(string unique)
        {
            string uniqueId = string.Empty;

            var result = Process.ExtractOne(unique, _uniqueNames, scorer: ScorerCache.Get<WeightedRatioScorer>());
            uniqueId = _uniqueMapNameToId[result.Value];

            return new ItemAffix
            {
                Id = uniqueId,
                Type = string.Empty,
                Color = _settingsManager.Settings.DefaultColorUniques
            };
        }

        private void ExportBuildVariants(MobalyticsBuild mobalyticsBuild, MobalyticsBuildJson mobalyticsBuildJson)
        {
            foreach (var buildVariant in mobalyticsBuildJson.Data.Game.Documents.UserGeneratedDocumentById.Data.Data.BuildVariants.values)
            {
                var variantNames = mobalyticsBuildJson.Data.Game.Documents.UserGeneratedDocumentById.Data.Content
                    .FirstOrDefault(v => v.Typename.Equals("NgfDocumentCmWidgetContentVariantsV1")) ?? new MobalyticsBuildUserGeneratedDocumentByIdDataContentJson();
                var variantName = variantNames.Data.ChildrenVariants.FirstOrDefault(v => v.Id.Equals(buildVariant.Id))?.Title ?? string.Empty;

                if (string.IsNullOrWhiteSpace(variantName))
                {
                    variantName = mobalyticsBuildJson.Data.Game.Documents.UserGeneratedDocumentById.Data.Data.Name;
                }

                ExportBuildVariant(variantName, mobalyticsBuild, buildVariant);
            }
        }

        private void ExportBuildVariant(string variantName, MobalyticsBuild mobalyticsBuild, MobalyticsBuildDataBuildVariantJson buildVariant)
        {
            WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
            {
                Build = mobalyticsBuild,
                Status = $"Exporting {variantName}."
            }));

            var mobalyticsBuildVariant = new MobalyticsBuildVariant
            {
                Name = variantName
            };

            mobalyticsBuildVariant.Aspect = GetAllAspects(buildVariant);
            mobalyticsBuildVariant.Uniques = GetAllUniques(buildVariant);

            // Armor
            mobalyticsBuildVariant.Helm = GetAllAffixes(buildVariant, "helm");
            mobalyticsBuildVariant.Chest = GetAllAffixes(buildVariant, "chest-armor");
            mobalyticsBuildVariant.Gloves = GetAllAffixes(buildVariant, "gloves");
            mobalyticsBuildVariant.Pants = GetAllAffixes(buildVariant, "pants");
            mobalyticsBuildVariant.Boots = GetAllAffixes(buildVariant, "boots");

            // Accessories
            mobalyticsBuildVariant.Amulet = GetAllAffixes(buildVariant, "amulet");
            mobalyticsBuildVariant.Ring.AddRange(GetAllAffixes(buildVariant, "ring-1"));
            mobalyticsBuildVariant.Ring.AddRange(GetAllAffixes(buildVariant, "ring-2"));
            mobalyticsBuildVariant.Ring = mobalyticsBuildVariant.Ring.Distinct().ToList();

            // Weapons
            mobalyticsBuildVariant.Weapon.AddRange(GetAllAffixes(buildVariant, "bludgeoning-weapon"));
            mobalyticsBuildVariant.Weapon.AddRange(GetAllAffixes(buildVariant, "dual-wield-weapon-1"));
            mobalyticsBuildVariant.Weapon.AddRange(GetAllAffixes(buildVariant, "dual-wield-weapon-2"));
            mobalyticsBuildVariant.Weapon.AddRange(GetAllAffixes(buildVariant, "slashing-weapon"));
            mobalyticsBuildVariant.Weapon.AddRange(GetAllAffixes(buildVariant, "weapon"));
            mobalyticsBuildVariant.Weapon = mobalyticsBuildVariant.Weapon.Distinct().ToList();
            mobalyticsBuildVariant.Offhand = GetAllAffixes(buildVariant, "offhand");
            mobalyticsBuildVariant.Ranged = GetAllAffixes(buildVariant, "ranged-weapon");

            // Runes
            mobalyticsBuildVariant.Runes = GetAllRunes(buildVariant);

            // Paragon Boards
            if (_settingsManager.Settings.IsImportParagonMobalyticsEnabled)
            {
                mobalyticsBuildVariant.ParagonBoards = GetAllParagonBoards(buildVariant);
            }

            mobalyticsBuild.Variants.Add(mobalyticsBuildVariant);
            WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
            {
                Build = mobalyticsBuild,
                Status = $"Exported {variantName}."
            }));
        }

        private void FinalizeBuildDownload()
        {
            // Kill process because of issue with lingering Chrome processes.
            var process = System.Diagnostics.Process.GetProcesses().FirstOrDefault(p => p.Id == _webDriverProcessId);
            process?.Kill(true);
            process?.WaitForExit(1000);

            // The following fix to close Chrome processes the correct way does not always work.
            // Note: You need to call driver.close() before driver.quit() otherwise you get lingering chrome processes with high resource usage.
            // This is an issue with recent chrome versions (124+).
            //_webDriver?.Close(); // Can't use Close() in combination with process?.Kill(true).
            _webDriver?.Quit();
            _webDriver?.Dispose();
            _webDriver = null;
            _webDriverWait = null;

            _timerTimeout.Stop();
           
            WeakReferenceMessenger.Default.Send(new MobalyticsCompletedMessage());
        }

        private List<MobalyticsAffix> GetAllAffixes(MobalyticsBuildDataBuildVariantJson buildVariant, string itemType)
        {
            try
            {
                List<MobalyticsAffix> affixes = new List<MobalyticsAffix>();

                // Find item slot that matches itemType
                var itemSlot = buildVariant.GenericBuilder.Slots?.FirstOrDefault(item => item.GameSlotSlug.Equals(itemType, StringComparison.OrdinalIgnoreCase));
                if (itemSlot == null) return affixes;

                bool isUniqueItem = itemSlot.GameEntity.Type.Equals("uniqueItems", StringComparison.OrdinalIgnoreCase);

                // Explicit
                foreach (var affix in itemSlot.GameEntity.Modifiers?.GearStats ?? Enumerable.Empty<MobalyticsBuildModifiersGearStatJson>())
                {
                    if (affix == null) continue;

                    MobalyticsAffix mobalyticsAffix = new MobalyticsAffix();
                    mobalyticsAffix.IsGreater = affix.IsGreater;
                    mobalyticsAffix.IsImplicit = false;
                    mobalyticsAffix.IsTempered = false;
                    mobalyticsAffix.AffixText = affix.Id;
                    mobalyticsAffix.AffixTextList = affix.Id.Split('-').ToList();
                    affixes.Add(mobalyticsAffix);
                }

                // Implicit
                foreach (var affix in itemSlot.GameEntity.Modifiers?.ImplicitStats ?? Enumerable.Empty<MobalyticsBuildModifiersImplicitStatJson>())
                {
                    if (affix == null) continue;

                    MobalyticsAffix mobalyticsAffix = new MobalyticsAffix();
                    mobalyticsAffix.IsGreater = false;
                    mobalyticsAffix.IsImplicit = true;
                    mobalyticsAffix.IsTempered = false;
                    mobalyticsAffix.AffixText = affix.Id;
                    mobalyticsAffix.AffixTextList = affix.Id.Split('-').ToList();
                    affixes.Add(mobalyticsAffix);
                }

                // Tempered
                foreach (var affix in itemSlot.GameEntity.Modifiers?.TemperingStats ?? Enumerable.Empty<MobalyticsBuildModifiersTemperingStatJson>())
                {
                    if (affix == null) continue;

                    MobalyticsAffix mobalyticsAffix = new MobalyticsAffix();
                    mobalyticsAffix.IsGreater = false;
                    mobalyticsAffix.IsImplicit = false;
                    mobalyticsAffix.IsTempered = true;
                    mobalyticsAffix.AffixText = affix.Id;
                    mobalyticsAffix.AffixTextList = affix.Id.Split('-').ToList();
                    affixes.Add(mobalyticsAffix);
                }
                return affixes;
            }
            catch (Exception)
            {
                return new();
            }
        }

        private List<string> GetAllAspects(MobalyticsBuildDataBuildVariantJson buildVariant)
        {
            List<string> aspects = new List<string>();

            var itemSlotsWithAspect = buildVariant.GenericBuilder.Slots?.FindAll(item => item.GameEntity.Type.Equals("aspects", StringComparison.OrdinalIgnoreCase)) ?? 
                Enumerable.Empty<MobalyticsBuildGenericBuilderSlotJson>().ToList();
            // Note: item.GameEntity.Title sometimes null or empty.
            //aspects.AddRange(itemSlotsWithAspect.Select(item => item.GameEntity.Title));
            aspects.AddRange(itemSlotsWithAspect.Select(item => item.GameEntity.Slug.Replace("aspect", string.Empty).Replace("-", " ")));

            return aspects;
        }

        private List<string> GetAllRunes(MobalyticsBuildDataBuildVariantJson buildVariant)
        {
            List<string> runes = new List<string>();

            var itemSlotsWithRune = buildVariant.GenericBuilder.Slots?
                .FindAll(item => item.GameEntity.Modifiers?.SocketStats != null &&
                                 item.GameEntity.Modifiers.SocketStats.Any(s => s != null && s.Type.Equals("runes"))) ??
                Enumerable.Empty<MobalyticsBuildGenericBuilderSlotJson>().ToList();

            runes.AddRange(itemSlotsWithRune.SelectMany(item => item.GameEntity.Modifiers.SocketStats
                .Where(s => s.Type.Equals("runes"))
                .Select(s => s.Slug)));

            return runes;
        }

        private List<string> GetAllUniques(MobalyticsBuildDataBuildVariantJson buildVariant)
        {
            List<string> uniques = new List<string>();

            var itemSlotsWithUnique = buildVariant.GenericBuilder.Slots?.FindAll(item => item.GameEntity.Type.Equals("uniqueItems", StringComparison.OrdinalIgnoreCase)) ??
                Enumerable.Empty<MobalyticsBuildGenericBuilderSlotJson>().ToList();
            // Note: item.GameEntity.Title sometimes null or empty.
            //uniques.AddRange(itemSlotsWithUnique.Select(item => item.GameEntity.Title));
            uniques.AddRange(itemSlotsWithUnique.Select(item => item.GameEntity.Slug.Replace("-", " ")));

            return uniques;
        }

        private List<ParagonBoard> GetAllParagonBoards(MobalyticsBuildDataBuildVariantJson buildVariant)
        {
            List<ParagonBoard> paragonBoards = new List<ParagonBoard>();
            if (buildVariant.Paragon == null || buildVariant.Paragon.Boards == null) return paragonBoards;
            
            foreach (MobalyticsBuildParagonBoardJson board in buildVariant.Paragon.Boards)
            {
                var paragonBoard = new ParagonBoard();
                paragonBoards.Add(paragonBoard);

                paragonBoard.Name = board.Board.Slug;
                paragonBoard.Glyph = board.Glyph?.Slug ?? string.Empty;

                int rotation = board.Rotation % 360;
                paragonBoard.Rotation = rotation == 0 ? "0°" :
                                        rotation == 90 ? "90°" :
                                        rotation == 180 ? "180°" :
                                        rotation == 270 ? "270°" : "?°";

                var boardNodes = buildVariant.Paragon.Nodes.Where(n => n.Slug.StartsWith(paragonBoard.Name))?.ToList() ?? 
                    Enumerable.Empty<MobalyticsBuildParagonNodeJson>().ToList();

                if (boardNodes.Count == 0)
                {
                    WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                    {
                        Message = $"No nodes found for paragon board {paragonBoard.Name}."
                    }));
                }

                foreach (var node in boardNodes)
                {
                    string nodePosition = node.Slug.Replace(paragonBoard.Name + "-", string.Empty);

                    int locationX = int.Parse(nodePosition.Split("-")[0].Substring(1));
                    int locationY = int.Parse(nodePosition.Split("-")[1].Substring(1));
                    int locationXT = locationX;
                    int locationYT = locationY;

                    if (rotation == 0 ||
                        rotation == 360)
                    {
                        locationXT = locationXT - 1;
                        locationYT = locationYT - 1;
                    }
                    else if (rotation == 90)
                    {
                        locationXT = 21 - locationY;
                        locationYT = locationX;
                        locationYT = locationYT - 1;
                    }
                    else if (rotation == 180)
                    {
                        locationXT = 21 - locationX;
                        locationYT = 21 - locationY;
                    }
                    else if (rotation == 270)
                    {
                        locationXT = locationY;
                        locationYT = 21 - locationX;
                        locationXT = locationXT - 1;
                    }
                    paragonBoard.Nodes[locationYT * 21 + locationXT] = true;
                }
            }

            return paragonBoards;
        }

        private bool HasBuildUrlSlugFormat(string buildUrl)
        {
            bool result = false;

            // Format: https://mobalytics.gg/diablo-4/builds/warlock-dread-claws
            if (!buildUrl.Contains("/profile/") && buildUrl.Contains("/builds/"))
            {                
                result = true;
            }

            // Format: https://mobalytics.gg/diablo-4/profile/p4wnyhof/builds/tyrants-grasp-spam-warlock
            if (buildUrl.Contains("/profile/") && buildUrl.Contains("/builds/"))
            {
                // Should not contain a build and profile id.
                List<string> parts = buildUrl.Split("/").ToList();
                parts.RemoveAll(p => !(p.Length == 36 && p.Count(c => c == '-') == 4));

                result = parts.Count == 0;
            }

            return result;
        }

        private void LoadAvailableMobalyticsBuilds()
        {
            try
            {
                MobalyticsBuilds.Clear();

                string directory = @".\Builds\Mobalytics";
                if (Directory.Exists(directory))
                {
                    var fileEntries = Directory.EnumerateFiles(directory).Where(tooltip => tooltip.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                    foreach (string fileName in fileEntries)
                    {
                        string json = File.ReadAllText(fileName);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            MobalyticsBuild? mobalyticsBuild = JsonSerializer.Deserialize<MobalyticsBuild>(json);
                            if (mobalyticsBuild != null)
                            {
                                MobalyticsBuilds.Add(mobalyticsBuild);
                            }
                        }
                    }

                    WeakReferenceMessenger.Default.Send(new MobalyticsBuildsLoadedMessage());
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void LoadAvailableMobalyticsProfiles()
        {
            try
            {
                MobalyticsProfiles.Clear();

                string directory = @".\Profiles\Mobalytics";
                if (Directory.Exists(directory))
                {
                    var fileEntries = Directory.EnumerateFiles(directory).Where(tooltip => tooltip.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                    foreach (string fileName in fileEntries)
                    {
                        string json = File.ReadAllText(fileName);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            MobalyticsProfile? mobalyticsProfile = JsonSerializer.Deserialize<MobalyticsProfile>(json);
                            if (mobalyticsProfile != null)
                            {
                                MobalyticsProfiles.Add(mobalyticsProfile);
                            }
                        }
                    }

                    WeakReferenceMessenger.Default.Send(new MobalyticsProfilesLoadedMessage());
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private List<MobalyticsBuildWrapperQueryData>? ObjectToMobalyticsBuildWrapperQueryData(object? value)
        {
            var deserializeOptions = new JsonSerializerOptions();
            deserializeOptions.Converters.Add(new BoolConverter());
            deserializeOptions.Converters.Add(new IntConverter());

            if (value is JsonElement el && el.ValueKind == JsonValueKind.Array)
            {
                try
                {
                    return JsonSerializer.Deserialize<List<MobalyticsBuildWrapperQueryData>>(el.GetRawText(), deserializeOptions);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private void ParseJsonBuild(string json)
        {
            var deserializeOptions = new JsonSerializerOptions();
            deserializeOptions.Converters.Add(new BoolConverter());
            deserializeOptions.Converters.Add(new IntConverter());
            MobalyticsBuildJson? mobalyticsBuildJson = JsonSerializer.Deserialize<MobalyticsBuildJson>(json, deserializeOptions);
            if (mobalyticsBuildJson != null)
            {
                // Valid json - Convert to MobalyticsBuild
                MobalyticsBuild mobalyticsBuild = new MobalyticsBuild
                {
                    Id = mobalyticsBuildJson.Data.Game.Documents.UserGeneratedDocumentById.Data.Id,
                    Url = _buildUrl,
                    Name = mobalyticsBuildJson.Data.Game.Documents.UserGeneratedDocumentById.Data.Data.Name,
                    Date = mobalyticsBuildJson.Data.Game.Documents.UserGeneratedDocumentById.Data.UpdatedAt
                };

                WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                {
                    Build = mobalyticsBuild,
                    Status = $"Exporting {mobalyticsBuild.Name}."
                }));

                ExportBuildVariants(mobalyticsBuild, mobalyticsBuildJson);
                ConvertBuildVariants(mobalyticsBuild);

                // Save build
                Directory.CreateDirectory(@".\Builds\Mobalytics");
                using (FileStream stream = File.Create(@$".\Builds\Mobalytics\{mobalyticsBuild.Id}.json"))
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    JsonSerializer.Serialize(stream, mobalyticsBuild, options);
                }
                LoadAvailableMobalyticsBuilds();

                WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                {
                    Build = mobalyticsBuild,
                    Status = $"Done."
                }));
            }

            FinalizeBuildDownload();
        }

        private void ParseJsonBuildBySlug(string json)
        {
            var deserializeOptions = new JsonSerializerOptions();
            deserializeOptions.Converters.Add(new BoolConverter());
            deserializeOptions.Converters.Add(new IntConverter());
            MobalyticsBuildWrapperJson? mobalyticsBuildWrapperJson = JsonSerializer.Deserialize<MobalyticsBuildWrapperJson>(json, deserializeOptions);          
            MobalyticsBuildWrapperQuery? mobalyticsBuildWrapperQuery = mobalyticsBuildWrapperJson?.Apollo.GraphqlV2.Queries.FirstOrDefault(q => !string.IsNullOrWhiteSpace(ObjectToMobalyticsBuildWrapperQueryData(q.State.Data)?[0].Game.Documents.UserGeneratedDocumentBySlug.Data.Id));
            MobalyticsBuildUserGeneratedDocumentByIdJson? mobalyticsBuildUserGeneratedDocumentByIdJson = mobalyticsBuildWrapperQuery == null ? null : ObjectToMobalyticsBuildWrapperQueryData(mobalyticsBuildWrapperQuery.State.Data)?[0].Game.Documents.UserGeneratedDocumentBySlug;

            if (mobalyticsBuildUserGeneratedDocumentByIdJson == null)
            {
                mobalyticsBuildWrapperQuery = mobalyticsBuildWrapperJson?.Apollo.GraphqlV2.Queries.FirstOrDefault(q => !string.IsNullOrWhiteSpace(ObjectToMobalyticsBuildWrapperQueryData(q.State.Data)?[0].Game.Documents.UserGeneratedDocumentBySlugifiedName.Data.Id));
                mobalyticsBuildUserGeneratedDocumentByIdJson = mobalyticsBuildWrapperQuery == null ? null : ObjectToMobalyticsBuildWrapperQueryData(mobalyticsBuildWrapperQuery.State.Data)?[0].Game.Documents.UserGeneratedDocumentBySlugifiedName;
            }

            if (mobalyticsBuildUserGeneratedDocumentByIdJson != null)
            {
                MobalyticsBuildJson mobalyticsBuildJson = new();
                mobalyticsBuildJson.Data.Game.Documents.UserGeneratedDocumentById = mobalyticsBuildUserGeneratedDocumentByIdJson;

                // Valid json - Convert to MobalyticsBuild
                MobalyticsBuild mobalyticsBuild = new MobalyticsBuild
                {
                    Id = mobalyticsBuildJson.Data.Game.Documents.UserGeneratedDocumentById.Data.Id,
                    Url = _buildUrl,
                    Name = mobalyticsBuildJson.Data.Game.Documents.UserGeneratedDocumentById.Data.Data.Name,
                    Date = mobalyticsBuildJson.Data.Game.Documents.UserGeneratedDocumentById.Data.UpdatedAt
                };

                WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                {
                    Build = mobalyticsBuild,
                    Status = $"Exporting {mobalyticsBuild.Name}."
                }));

                ExportBuildVariants(mobalyticsBuild, mobalyticsBuildJson);
                ConvertBuildVariants(mobalyticsBuild);

                // Save build
                Directory.CreateDirectory(@".\Builds\Mobalytics");
                using (FileStream stream = File.Create(@$".\Builds\Mobalytics\{mobalyticsBuild.Id}.json"))
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    JsonSerializer.Serialize(stream, mobalyticsBuild, options);
                }
                LoadAvailableMobalyticsBuilds();

                WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                {
                    Build = mobalyticsBuild,
                    Status = $"Done."
                }));
            }

            FinalizeBuildDownload();
        }

        private void ParseJsonProfile(string url, string json)
        {
            MobalyticsProfileJson? mobalyticsProfileJson = JsonSerializer.Deserialize<MobalyticsProfileJson>(json);
            if (mobalyticsProfileJson != null)
            {
                // Valid json - Convert to MobalyticsProfile
                var queryBuilds = mobalyticsProfileJson.Apollo.Graphql.Queries.FirstOrDefault(q => q.QueryKeys.Any(k => (k?.ToString() ?? string.Empty) == "ngf-creator-profile-documents"));
                var queryProfile = mobalyticsProfileJson.Apollo.Graphql.Queries.FirstOrDefault(q => q.QueryKeys.Any(k => (k?.ToString() ?? string.Empty) == "mgp-header"));

                if (queryBuilds != null && queryProfile != null)
                {
                    string queryBuildsJsonString = JsonSerializer.Serialize(queryBuilds.State);
                    string queryProfileJsonString = JsonSerializer.Serialize(queryProfile.State);

                    var queryStateBuilds = JsonSerializer.Deserialize<MobalyticsProfileStateBuildsJson>(queryBuildsJsonString) ?? new();
                    var queryStateProfile = JsonSerializer.Deserialize<MobalyticsProfileStateProfileJson>(queryProfileJsonString) ?? new();

                    string profileId = queryStateProfile.DataList[0].Mgp.Profile.Data.User.Id;
                    string name = queryStateProfile.DataList[0].Mgp.Profile.Data.User.DisplayName;
                    string profileName = queryStateProfile.DataList[0].Mgp.Profile.Data.User.Username;

                    // Parse url
                    string filters = url.Split('?').Length > 1 ? url.Split('?')[1] : string.Empty;
                    List<string> filterList = filters.Split('&').ToList();
                    foreach (var filter in filterList)
                    {
                        if (!filter.Contains("=")) continue;
                        string value = filter.Split('=')[1];
                        name = $"{name} - {value}";
                    }

                    MobalyticsProfile mobalyticsProfile = new MobalyticsProfile
                    {
                        Id = profileId,
                        Name = name,
                        ProfileName = profileName,
                        Url = url
                    };

                    WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                    {
                        Profile = mobalyticsProfile,
                        Status = $"Exporting {mobalyticsProfile.Name}."
                    }));

                    foreach (var build in queryStateBuilds.Data.Pages[0][0].Game.Documents.UserGeneratedDocuments.Documents)
                    {
                        if (build == null) continue;

                        var mobalyticsBuildVariant = new MobalyticsProfileBuildVariant
                        {
                            Date = build.UpdatedAt,
                            Id = build.Id,
                            Name = build.Data.Name,
                            Url = $"https://mobalytics.gg/diablo-4/profile/{profileName}/builds/{build.SlugifiedName ?? build.Id}"
                        };
                        mobalyticsProfile.Variants.Add(mobalyticsBuildVariant);

                        WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                        {
                            Profile = mobalyticsProfile,
                            Status = $"Exported {build.Data.Name}."
                        }));
                    }

                    // Sort builds by date
                    mobalyticsProfile.Variants.Sort((x, y) => DateTime.Parse(y.Date).CompareTo(DateTime.Parse(x.Date)));

                    // Save build
                    Directory.CreateDirectory(@".\Profiles\Mobalytics");
                    using (FileStream stream = File.Create(@$".\Profiles\Mobalytics\{name}.json"))
                    {
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        JsonSerializer.Serialize(stream, mobalyticsProfile, options);
                    }
                    LoadAvailableMobalyticsProfiles();

                    WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                    {
                        Profile = mobalyticsProfile,
                        Status = $"Done."
                    }));
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                    {
                        Status = $"Failed. Invalid json."
                    }));
                }
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new MobalyticsStatusUpdateMessage(new MobalyticsStatusUpdateMessageParams
                {
                    Status = $"Failed. Invalid json."
                }));
            }

            FinalizeBuildDownload();
        }

        public void RemoveMobalyticsBuild(string buildId)
        {
            try
            {
                string directory = @".\Builds\Mobalytics";
                File.Delete(@$"{directory}\{buildId}.json");
                LoadAvailableMobalyticsBuilds();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        public void RemoveMobalyticsProfile(string profileIdName)
        {
            try
            {
                string directory = @".\Profiles\Mobalytics";
                File.Delete(@$"{directory}\{profileIdName}.json");
                LoadAvailableMobalyticsProfiles();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion
    }
}
