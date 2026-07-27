using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using D4Companion.Messages;
using Emgu.CV.Aruco;
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
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace D4Companion.Services
{
    public class BuildsManagerInfinityBuilds : IBuildsManagerInfinityBuilds
    {
        private readonly IAffixManager _affixManager;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private List<AffixInfo> _affixes = new List<AffixInfo>();
        private List<string> _affixDescriptions = new List<string>();
        private Dictionary<string, string> _affixMapDescriptionToId = new Dictionary<string, string>(); // Currently not useable because InfinityBuilds uses affix ids instead of names.
        private List<AspectInfo> _aspects = new List<AspectInfo>();
        private List<string> _aspectIds = new List<string>();
        private List<string> _aspectNames = new List<string>();
        private Dictionary<string, string> _aspectMapNameToId = new Dictionary<string, string>(); // Currently not useable because InfinityBuilds mixes English and Chinese aspect names.
        private string _buildUrl = string.Empty;        
        private List<InfinityBuildsBuild> _infinityBuildsBuilds = new();
        private List<RuneInfo> _runes = new List<RuneInfo>();
        private List<string> _runeId = new List<string>();
        private List<string> _runeNames = new List<string>();
        private Dictionary<string, string> _runeMapNameToId = new Dictionary<string, string>();
        private System.Timers.Timer _timerTimeout = new();
        private List<UniqueInfo> _uniques = new List<UniqueInfo>();
        private List<string> _uniqueIds = new List<string>();
        private List<string> _uniqueNames = new List<string>();
        private Dictionary<string, string> _uniqueMapNameToId = new Dictionary<string, string>(); // Currently not useable because InfinityBuilds mixes English and Chinese unique names.

        private object _lockTimerTimeout = new();
        private ChromeDriver? _webDriver = null;
        private DevToolsSession? _devToolsSession = null;
        private WebDriverWait? _webDriverWait = null;
        private int _webDriverProcessId = 0;

        // Start of Constructors region

        #region Constructors

        public BuildsManagerInfinityBuilds(ILogger<BuildsManagerInfinityBuilds> logger, IAffixManager affixManager, ISettingsManager settingsManager)
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

            // Load available InfinityBuilds builds and profiles.
            Task.Factory.StartNew(() =>
            {
                LoadAvailableInfinityBuildsBuilds();
            });
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<InfinityBuildsBuild> InfinityBuildsBuilds { get => _infinityBuildsBuilds; set => _infinityBuildsBuilds = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void TimerTimeoutElapsedHandler(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _timerTimeout.Stop();

            WeakReferenceMessenger.Default.Send(new InfinityBuildsStatusUpdateMessage(new InfinityBuildsStatusUpdateMessageParams
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
                // Remove class restrictions from description. InfinityBuilds does not show this information.
                return affix.DescriptionClean.Contains(")") ? affix.DescriptionClean.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries)[0] : affix.DescriptionClean;
            }).ToList();

            // Create dictionary to map affix description with affix id
            _affixMapDescriptionToId.Clear();
            _affixMapDescriptionToId = _affixes.ToDictionary(affix =>
            {
                // Remove class restrictions from description. InfinityBuilds does not show this information.
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

            // Create aspect lists for FuzzierSharp
            _aspectIds.Clear();
            _aspectNames.Clear();
            _aspectIds = _aspects.Select(aspect => aspect.IdName).ToList();
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
            _runeId.Clear();
            _runeNames.Clear();
            _runeId = _runes.Select(rune => rune.IdName).ToList();
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

            // Create unique lists for FuzzierSharp
            _uniqueIds.Clear();
            _uniqueNames.Clear();
            _uniqueIds = _uniques.Select(unique => unique.IdName).ToList();
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

            // Note: ChromeDriver 129 is bugged and causes blank window when using headless mode. Test again with the release of 130.
            //options.AddArgument("--headless=old"); //v129 and older
            options.AddArgument("--headless"); // v130+

            // Note: ChromeDriver DevToolsActivePort file doesn't exist exceptions. Below fix might be needed in combination with "--headless=old"
            // https://issues.chromium.org/issues/42323434#comment36
            //options.AddArgument("--remote-debugging-pipe");

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

            //options.AddArgument("--user-agent=Diablo4Companion/1.0");

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

        public void CreatePresetFromInfinityBuildsBuild(InfinityBuildsBuildVariant infinityBuildsBuild, string buildNameOriginal, string buildName)
        {
            buildName = string.IsNullOrWhiteSpace(buildName) ? buildNameOriginal : buildName;

            // Note: Only allow one InfinityBuilds build. Update if already exists.
            _affixManager.AffixPresets.RemoveAll(p => p.Name.Equals(buildName));

            var affixPreset = infinityBuildsBuild.AffixPreset.Clone();
            affixPreset.Name = buildName;

            _affixManager.AddAffixPreset(affixPreset);
        }

        public void DownloadInfinityBuildsBuild(string buildUrl)
        {
            try
            {
                WeakReferenceMessenger.Default.Send(new InfinityBuildsStatusUpdateMessage(new InfinityBuildsStatusUpdateMessageParams
                {
                    Status = $"Preparing browser instance."
                }));

                _buildUrl = buildUrl;

                if (_webDriver == null) InitSelenium();
                if (_webDriver == null) throw new Exception("WebDriver initialization failed.");
                if (_webDriverWait == null) throw new Exception("WebDriverWait initialization failed.");
                if (_devToolsSession == null) throw new Exception("DevToolsSession initialization failed.");

                WeakReferenceMessenger.Default.Send(new InfinityBuildsStatusUpdateMessage(new InfinityBuildsStatusUpdateMessageParams
                {
                    Status = $"Downloading {buildUrl}."
                }));
                _webDriver.Navigate().GoToUrl(buildUrl);

                // Wait until all required resources are loaded
                string? scriptContent = _webDriverWait.Until(d =>
                {
                    var js = (IJavaScriptExecutor)d;
                    return (string?)js.ExecuteScript(@"
                        const scripts = Array.from(document.scripts);
                        const match = scripts.find(s => s.textContent.includes('shareSlug'));
                        return match ? match.innerHTML : null;
                    ");
                });

                if (!string.IsNullOrWhiteSpace(scriptContent))
                {
                    // Remove self.__next_f.push
                    scriptContent = scriptContent.Substring(scriptContent.IndexOf("(") + 1);
                    scriptContent = scriptContent.Remove(scriptContent.Length - 1);

                    var json = JsonSerializer.Deserialize<object[]>(scriptContent) ?? [];
                    if (json.Length > 1)
                    {
                        var jsonAsString = json[1].ToString();
                        jsonAsString = jsonAsString?.Substring(jsonAsString.IndexOf("[")) ?? string.Empty;
                        ParseJsonBuild(jsonAsString);
                    }                    
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{MethodBase.GetCurrentMethod()?.Name} ({buildUrl})");

                WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                {
                    Message = $"Failed to download from InfinityBuilds ({buildUrl})"
                }));

                WeakReferenceMessenger.Default.Send(new InfinityBuildsStatusUpdateMessage(new InfinityBuildsStatusUpdateMessageParams
                {
                    Status = $"Failed. See log."
                }));

                FinalizeBuildDownload();
            }
        }

        private void ConvertBuildVariants(InfinityBuildsBuild infinityBuildsBuild)
        {
            foreach (var variant in infinityBuildsBuild.Variants)
            {
                WeakReferenceMessenger.Default.Send(new InfinityBuildsStatusUpdateMessage(new InfinityBuildsStatusUpdateMessageParams
                {
                    Build = infinityBuildsBuild,
                    Status = $"Converting {variant.Name}."
                }));

                var affixPreset = new AffixPreset
                {
                    Name = variant.Name
                };

                // Prepare affixes
                List<Tuple<string, InfinityBuildsAffix>> affixesInfinityBuilds = new List<Tuple<string, InfinityBuildsAffix>>();

                foreach (var affixInfinityBuilds in variant.Helm)
                {
                    affixesInfinityBuilds.Add(new Tuple<string, InfinityBuildsAffix>(Constants.ItemTypeConstants.Helm, affixInfinityBuilds));
                }
                foreach (var affixInfinityBuilds in variant.Chest)
                {
                    affixesInfinityBuilds.Add(new Tuple<string, InfinityBuildsAffix>(Constants.ItemTypeConstants.Chest, affixInfinityBuilds));
                }
                foreach (var affixInfinityBuilds in variant.Gloves)
                {
                    affixesInfinityBuilds.Add(new Tuple<string, InfinityBuildsAffix>(Constants.ItemTypeConstants.Gloves, affixInfinityBuilds));
                }
                foreach (var affixInfinityBuilds in variant.Pants)
                {
                    affixesInfinityBuilds.Add(new Tuple<string, InfinityBuildsAffix>(Constants.ItemTypeConstants.Pants, affixInfinityBuilds));
                }
                foreach (var affixInfinityBuilds in variant.Boots)
                {
                    affixesInfinityBuilds.Add(new Tuple<string, InfinityBuildsAffix>(Constants.ItemTypeConstants.Boots, affixInfinityBuilds));
                }
                foreach (var affixInfinityBuilds in variant.Amulet)
                {
                    affixesInfinityBuilds.Add(new Tuple<string, InfinityBuildsAffix>(Constants.ItemTypeConstants.Amulet, affixInfinityBuilds));
                }
                foreach (var affixInfinityBuilds in variant.Ring)
                {
                    affixesInfinityBuilds.Add(new Tuple<string, InfinityBuildsAffix>(Constants.ItemTypeConstants.Ring, affixInfinityBuilds));
                }
                foreach (var affixInfinityBuilds in variant.Weapon)
                {
                    affixesInfinityBuilds.Add(new Tuple<string, InfinityBuildsAffix>(Constants.ItemTypeConstants.Weapon, affixInfinityBuilds));
                }
                foreach (var affixInfinityBuilds in variant.Ranged)
                {
                    affixesInfinityBuilds.Add(new Tuple<string, InfinityBuildsAffix>(Constants.ItemTypeConstants.Ranged, affixInfinityBuilds));
                }
                foreach (var affixInfinityBuilds in variant.Offhand)
                {
                    affixesInfinityBuilds.Add(new Tuple<string, InfinityBuildsAffix>(Constants.ItemTypeConstants.Offhand, affixInfinityBuilds));
                }

                // Find matching affix ids
                ConcurrentBag<ItemAffix> itemAffixBag = new ConcurrentBag<ItemAffix>();
                Parallel.ForEach(affixesInfinityBuilds, affixInfinityBuilds =>
                {
                    var itemAffixResult = ConvertItemAffix(affixInfinityBuilds);
                    itemAffixBag.Add(itemAffixResult);
                });
                affixPreset.ItemAffixes.AddRange(itemAffixBag);
                affixPreset.ItemAffixes.RemoveAll(a => string.IsNullOrWhiteSpace(a.Id));

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
                affixPreset.ItemRunes.RemoveAll(r => string.IsNullOrWhiteSpace(r.Id));

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
                WeakReferenceMessenger.Default.Send(new InfinityBuildsStatusUpdateMessage(new InfinityBuildsStatusUpdateMessageParams
                {
                    Build = infinityBuildsBuild,
                    Status = $"Converted {variant.Name}."
                }));
            }
        }

        private ItemAffix ConvertItemAffix(Tuple<string, InfinityBuildsAffix> affixDescription)
        {
            // e.g. "affix-s04-corestat-intelligence"
            //      "affix-s04-life"
            //      "affix-s04-cooldownreductioncdr"
            //      "affix-tempered-generic-lifemax-tier3"
            //      "affix-s04-skillrankbonus-sorc-special-firewall"

            InfinityBuildsAffix infinityBuildsAffix = affixDescription.Item2;

            string infinityBuildsAffixId = infinityBuildsAffix.AffixText;
            infinityBuildsAffixId = infinityBuildsAffixId.Replace("-", "_");
            infinityBuildsAffixId = infinityBuildsAffixId.Substring(6); // Remove "affix_" prefix.           
            
            string itemType = affixDescription.Item1;
            string affixId = _affixes.FirstOrDefault(a => a.IdName.Contains(infinityBuildsAffixId, StringComparison.OrdinalIgnoreCase))?.IdName ?? string.Empty;

            Color color = infinityBuildsAffix.IsImplicit ? _settingsManager.Settings.DefaultColorImplicit :
                infinityBuildsAffix.IsGreater ? _settingsManager.Settings.DefaultColorGreater :
                infinityBuildsAffix.IsTempered ? _settingsManager.Settings.DefaultColorTempered :
                _settingsManager.Settings.DefaultColorNormal;
            return new ItemAffix
            {
                Id = affixId,
                Type = itemType,
                Color = color,
                IsGreater = infinityBuildsAffix.IsGreater,
                IsImplicit = infinityBuildsAffix.IsImplicit,
                IsTempered = infinityBuildsAffix.IsTempered
            };
        }

        private ItemAffix ConvertItemAspect(string aspect)
        {
            // e.g. "aspect-asp-legendary-paladin-031-asp"
            //      "aspect-asp-legendary-rogue-104-asp"
            //      "aspect-12-asp-legendary-spiritborn-016-asp"
            //      "aspect-265-asp-legendary-spiritborn-007-asp"
            //      "aspect-asp-s05-bsk-barbarian-001-x2-asp"

            string infinityBuildsAspectId = aspect;
            infinityBuildsAspectId = infinityBuildsAspectId.Replace("-", "_");

            if (infinityBuildsAspectId.Contains("legendary"))
            {
                infinityBuildsAspectId = infinityBuildsAspectId.Substring(infinityBuildsAspectId.IndexOf("legendary"));
            }
            else if(infinityBuildsAspectId.Contains("_asp_"))
            {
                infinityBuildsAspectId = infinityBuildsAspectId.Substring(infinityBuildsAspectId.IndexOf("_asp_") + 5);
            }                
            infinityBuildsAspectId = infinityBuildsAspectId.Remove(infinityBuildsAspectId.Length - 4); // Remove "_asp" suffix.

            // Issue with certain ids
            // InfinityBuilds uses legendary_spiritborn_040 instead of legendary_spiritborn_040_x1
            // The fuzzy search matches this then with legendary_spiritborn_050 instead of legendary_spiritborn_040_x1

            var result = Process.ExtractOne(infinityBuildsAspectId, _aspectIds, scorer: ScorerCache.Get<WeightedRatioScorer>());
            if (result.Score < 100)
            {
                result = Process.ExtractOne(infinityBuildsAspectId + "_x1", _aspectIds, scorer: ScorerCache.Get<DefaultRatioScorer>());
            }
            
            string aspectId = result.Value;

            return new ItemAffix
            {
                Id = aspectId,
                Type = Constants.ItemTypeConstants.Helm,
                Color = _settingsManager.Settings.DefaultColorAspects
            };
        }

        private ItemAffix ConvertItemRune(string rune)
        {
            // e.g. "item-rune-condition-summons-itm"
            //      "item-rune-effect-summonspiritwolf-itm"
            //      "item-rune-condition-bomb-itm"
            //      "item-rune-effect-rogue-darkshroud-itm"
            //      "item-9236-rune-condition-castrepeatskill-itm"
            //      "item-5653-rune-effect-critbuff-itm"
            //      "item-6372-rune-condition-onspendresource-itm"
            //      "item-1939-rune-effect-summonspiritwolf-itm"

            string infinityBuildsRuneId = rune;

            // Fix item names
            // - Replace "-" with "_"
            // - Remove "_itm" suffix
            // - Remove "item***rune_" prefix and replace with the correct "item_rune_" prefix.
            infinityBuildsRuneId = infinityBuildsRuneId.Replace("-", "_");
            infinityBuildsRuneId = infinityBuildsRuneId.Remove(infinityBuildsRuneId.Length - 4);
            infinityBuildsRuneId = infinityBuildsRuneId.Substring(infinityBuildsRuneId.IndexOf("_rune_") + 6);
            infinityBuildsRuneId = $"item_rune_{infinityBuildsRuneId}";

            string runeId = _runes.FirstOrDefault(r => r.IdName.Equals(infinityBuildsRuneId, StringComparison.OrdinalIgnoreCase))?.IdName ?? string.Empty;
            return new ItemAffix
            {
                Id = runeId,
                Type = Constants.ItemTypeConstants.Rune,
                Color = _settingsManager.Settings.DefaultColorRunes
            };
        }

        private ItemAffix ConvertItemUnique(string unique)
        {
            // e.g. "item-helm-unique-sorc-102-x2-itm"
            //      "item-chest-unique-sorc-002-itm"
            //      "item-pants-unique-generic-102-itm"
            //      "item-ring-unique-sorc-104-x2-itm"

            string infinityBuildsUniqueId = unique;
            infinityBuildsUniqueId = infinityBuildsUniqueId.Replace("-", "_");
            infinityBuildsUniqueId = infinityBuildsUniqueId.Substring(5); // Remove "item-" prefix.
            infinityBuildsUniqueId = infinityBuildsUniqueId.Remove(infinityBuildsUniqueId.Length - 4); // Remove "_itm" suffix.

            var result = Process.ExtractOne(infinityBuildsUniqueId, _uniqueIds, scorer: ScorerCache.Get<WeightedRatioScorer>());
            string uniqueId = result.Value;

            return new ItemAffix
            {
                Id = uniqueId,
                Type = string.Empty,
                Color = _settingsManager.Settings.DefaultColorUniques
            };
        }

        private void ExportBuildVariants(InfinityBuildsBuild infinityBuildsBuild, InfinityBuildsBuildJson infinityBuildsBuildJson)
        {
            foreach (var buildVariant in infinityBuildsBuildJson.Variants)
            {
                string variantName = buildVariant.Name;

                ExportBuildVariant(buildVariant.Name, infinityBuildsBuild, buildVariant);
            }
        }

        private void ExportBuildVariant(string variantName, InfinityBuildsBuild infinityBuildsBuild, InfinityBuildsBuildVariantJson buildVariant)
        {
            WeakReferenceMessenger.Default.Send(new InfinityBuildsStatusUpdateMessage(new InfinityBuildsStatusUpdateMessageParams
            {
                Build = infinityBuildsBuild,
                Status = $"Exporting {variantName}."
            }));

            var infinityBuildsBuildVariant = new InfinityBuildsBuildVariant
            {
                Name = variantName
            };

            infinityBuildsBuildVariant.Aspect = GetAllAspects(buildVariant);
            infinityBuildsBuildVariant.Uniques = GetAllUniques(buildVariant);

            // Armor
            infinityBuildsBuildVariant.Helm = GetAllAffixes(buildVariant, "helm");
            infinityBuildsBuildVariant.Chest = GetAllAffixes(buildVariant, "chest");
            infinityBuildsBuildVariant.Gloves = GetAllAffixes(buildVariant, "gloves");
            infinityBuildsBuildVariant.Pants = GetAllAffixes(buildVariant, "pants");
            infinityBuildsBuildVariant.Boots = GetAllAffixes(buildVariant, "boots");

            // Accessories
            infinityBuildsBuildVariant.Amulet = GetAllAffixes(buildVariant, "amulet");
            infinityBuildsBuildVariant.Ring.AddRange(GetAllAffixes(buildVariant, "ring1"));
            infinityBuildsBuildVariant.Ring.AddRange(GetAllAffixes(buildVariant, "ring2"));
            infinityBuildsBuildVariant.Ring = infinityBuildsBuildVariant.Ring.Distinct().ToList();

            // Weapons
            infinityBuildsBuildVariant.Weapon.AddRange(GetAllAffixes(buildVariant, "mainhand"));
            infinityBuildsBuildVariant.Weapon.AddRange(GetAllAffixes(buildVariant, "offhandWeapon"));
            infinityBuildsBuildVariant.Weapon.AddRange(GetAllAffixes(buildVariant, "twoHander"));
            infinityBuildsBuildVariant.Weapon.AddRange(GetAllAffixes(buildVariant, "weapon"));
            infinityBuildsBuildVariant.Weapon = infinityBuildsBuildVariant.Weapon.Distinct().ToList();
            infinityBuildsBuildVariant.Offhand = GetAllAffixes(buildVariant, "offhand");
            //infinityBuildsBuildVariant.Ranged = GetAllAffixes(buildVariant, "ranged-weapon"); // InfinityBuilds does not separates melee and ranged weapons.

            // Runes
            infinityBuildsBuildVariant.Runes = GetAllRunes(buildVariant);

            // Paragon Boards
            if (_settingsManager.Settings.IsImportParagonInfinityBuildsEnabled)
            {
                infinityBuildsBuildVariant.ParagonBoards = GetAllParagonBoards(buildVariant);
            }

            infinityBuildsBuild.Variants.Add(infinityBuildsBuildVariant);
            WeakReferenceMessenger.Default.Send(new InfinityBuildsStatusUpdateMessage(new InfinityBuildsStatusUpdateMessageParams
            {
                Build = infinityBuildsBuild,
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

            WeakReferenceMessenger.Default.Send(new InfinityBuildsCompletedMessage());
        }

        private List<InfinityBuildsAffix> GetAllAffixes(InfinityBuildsBuildVariantJson buildVariant, string itemType)
        {
            try
            {
                List<InfinityBuildsAffix> affixes = new List<InfinityBuildsAffix>();

                foreach (var gearEntry in buildVariant.Gear)
                {
                    if (!gearEntry.Slot.Equals(itemType, StringComparison.OrdinalIgnoreCase)) continue;

                    bool isUniqueItem = gearEntry.Kind.Equals("unique", StringComparison.OrdinalIgnoreCase) ||
                                        gearEntry.Kind.Equals("mythic", StringComparison.OrdinalIgnoreCase);

                    foreach (var affix in gearEntry.Affixes)
                    {
                        InfinityBuildsAffix infinityBuildsAffix = new InfinityBuildsAffix();
                        infinityBuildsAffix.IsGreater = affix.Greater;
                        infinityBuildsAffix.IsImplicit = false; // Not available
                        infinityBuildsAffix.IsTempered = affix.Tempered;
                        infinityBuildsAffix.AffixText = affix.AffixId;
                        infinityBuildsAffix.AffixTextList = affix.AffixId.Split('-').ToList();
                        affixes.Add(infinityBuildsAffix);
                    }
                }

                return affixes;                
            }
            catch (Exception)
            {
                return new();
            }
        }

        private List<string> GetAllAspects(InfinityBuildsBuildVariantJson buildVariant)
        {
            List<string> aspects = new List<string>();

            foreach (var gearEntry in buildVariant.Gear)
            {
                if (string.IsNullOrWhiteSpace(gearEntry.AspectId)) continue;

                // e.g. "aspect-asp-legendary-paladin-031-asp"
                //      "aspect-asp-legendary-rogue-104-asp"
                //      "aspect-12-asp-legendary-spiritborn-016-asp"
                //      "aspect-265-asp-legendary-spiritborn-007-asp"
                aspects.Add(gearEntry.AspectId);
            }

            return aspects;
        }

        private List<string> GetAllRunes(InfinityBuildsBuildVariantJson buildVariant)
        {
            List<string> runes = new List<string>();

            foreach (var gearEntry in buildVariant.Gear)
            {
                foreach (var socket in gearEntry.Sockets)
                {
                    if (!socket.Contains("-rune-", StringComparison.OrdinalIgnoreCase)) continue;

                    runes.Add(socket);
                }
            }

            return runes;
        }

        private List<string> GetAllUniques(InfinityBuildsBuildVariantJson buildVariant)
        {
            List<string> uniques = new List<string>();

            foreach (var gearEntry in buildVariant.Gear)
            {
                if (gearEntry.Kind.Equals("unique") ||
                    gearEntry.Kind.Equals("mythic"))
                {
                    uniques.Add(gearEntry.ItemId);
                }
            }

            return uniques;
        }

        private List<ParagonBoard> GetAllParagonBoards(InfinityBuildsBuildVariantJson buildVariant)
        {
            List<ParagonBoard> paragonBoards = new List<ParagonBoard>();            

            if (buildVariant.Paragon.Slots.Count == 0 || buildVariant.Paragon.ActiveNodes.Count == 0) return paragonBoards;

            // Starter board
            var paragonBoard = new ParagonBoard();
            paragonBoards.Add(paragonBoard);
            paragonBoard.Name = buildVariant.Paragon.ActiveNodes[0].Split("::")[1];
            paragonBoard.Rotation = "0°";

            // Other boards
            foreach (InfinityBuildsBuildParagonSlotJson board in buildVariant.Paragon.Slots)
            {
                paragonBoard = new ParagonBoard();
                paragonBoards.Add(paragonBoard);

                paragonBoard.Name = board.BoardId.Split("::")[1];
                paragonBoard.Rotation = board.Rotation == 0 ? "0°" :
                                        board.Rotation == 1 ? "90°" :
                                        board.Rotation == 2 ? "180°" :
                                        board.Rotation == 3 ? "270°" : "?°";
            }

            // Update boards
            foreach (var board in paragonBoards)
            {
                // Glyph
                var glyphs = buildVariant.Paragon.Glyphs.Keys.ToList() ?? new List<string>();
                var glyph = glyphs.FirstOrDefault(g => g.Contains("::" + board.Name + "::", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
                if (buildVariant.Paragon.Glyphs.ContainsKey(glyph))
                {
                    board.Glyph = buildVariant.Paragon.Glyphs[glyph];
                    board.Glyph = board.Glyph.Substring(7); // Remove "glyph::" prefix.
                }                

                // Nodes
                foreach (var node in buildVariant.Paragon.ActiveNodes)
                {
                    var nodeData = node.Split("::");
                    string nodeBoard = nodeData[1];
                    int nodeLocation = int.Parse(nodeData[2]);

                    if (!board.Name.Equals(nodeBoard)) continue;

                    int rotation = board.Rotation == "0°" ? 0 :
                                   board.Rotation == "90°" ? 1 :
                                   board.Rotation == "180°" ? 2 :
                                   board.Rotation == "270°" ? 3 : 0;

                    int locationT = nodeLocation;
                    int locationX = nodeLocation % 21;
                    int locationY = nodeLocation / 21;
                    int locationXT = locationX;
                    int locationYT = locationY;

                    switch (rotation)
                    {
                        case 0:
                            locationT = nodeLocation;
                            break;
                        case 1:
                            locationXT = 21 - locationY;
                            locationYT = locationX;
                            locationXT = locationXT - 1;
                            locationT = locationYT * 21 + locationXT;
                            break;
                        case 2:
                            locationXT = 21 - locationX;
                            locationYT = 21 - locationY;
                            locationXT = locationXT - 1;
                            locationYT = locationYT - 1;
                            locationT = locationYT * 21 + locationXT;
                            break;
                        case 3:
                            locationXT = locationY;
                            locationYT = 21 - locationX;
                            locationYT = locationYT - 1;
                            locationT = locationYT * 21 + locationXT;
                            break;
                        default:
                            locationT = nodeLocation;
                            break;
                    }

                    board.Nodes[locationT] = true;
                }
            }

            // Fix naming inconsistencies
            foreach (var board in paragonBoards)
            {
                board.Glyph = board.Glyph.Replace("-", "_");
                board.Name = board.Name.Replace("-", "_");
                board.Name = _affixManager.GetParagonBoardLocalisation(board.Name);
                board.Glyph = _affixManager.GetParagonGlyphLocalisation(board.Glyph);
            }

            return paragonBoards;
        }        

        private void LoadAvailableInfinityBuildsBuilds()
        {
            try
            {
                InfinityBuildsBuilds.Clear();

                string directory = @".\Builds\InfinityBuilds";
                if (Directory.Exists(directory))
                {
                    var fileEntries = Directory.EnumerateFiles(directory).Where(tooltip => tooltip.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                    foreach (string fileName in fileEntries)
                    {
                        string json = File.ReadAllText(fileName);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            InfinityBuildsBuild? infinityBuildsBuild = JsonSerializer.Deserialize<InfinityBuildsBuild>(json);
                            if (infinityBuildsBuild != null)
                            {
                                InfinityBuildsBuilds.Add(infinityBuildsBuild);
                            }
                        }
                    }

                    WeakReferenceMessenger.Default.Send(new InfinityBuildsBuildsLoadedMessage());
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void ParseJsonBuild(string json)
        {
            var deserializeOptions = new JsonSerializerOptions();
            deserializeOptions.Converters.Add(new BoolConverter());
            deserializeOptions.Converters.Add(new IntConverter());
            List<object>? infinityBuildsRootJson = JsonSerializer.Deserialize<List<object>>(json, deserializeOptions);
            if (infinityBuildsRootJson != null)
            {
                // Assume index 3 always contains the build data.
                var jsonAsString = JsonSerializer.Serialize(infinityBuildsRootJson[3]);
                InfinityBuildsContainerJson? infinityBuildsContainerJson = JsonSerializer.Deserialize<InfinityBuildsContainerJson>(jsonAsString, deserializeOptions);

                // Index of property Children containing build data varies.
                // Also there can be an extra nested property of Children.
                jsonAsString = string.Empty;
                for (int i = 0; i < infinityBuildsContainerJson?.Children.Count; i++)
                {
                    jsonAsString = JsonSerializer.Serialize(infinityBuildsContainerJson?.Children[i]);
                    if (jsonAsString.Contains("shareSlug")) break;
                }

                // Test for nested Children property - Nested if Id is not set to vm_content_container.
                if (string.IsNullOrWhiteSpace(infinityBuildsContainerJson?.Id))
                {
                    List<object>? infinityBuildsNestedRootJson = JsonSerializer.Deserialize<List<object>>(jsonAsString, deserializeOptions);
                    if (infinityBuildsNestedRootJson != null)
                    {
                        jsonAsString = JsonSerializer.Serialize(infinityBuildsNestedRootJson[3]);
                        infinityBuildsContainerJson = JsonSerializer.Deserialize<InfinityBuildsContainerJson>(jsonAsString, deserializeOptions);

                        for (int i = 0; i < infinityBuildsContainerJson?.Children.Count; i++)
                        {
                            jsonAsString = JsonSerializer.Serialize(infinityBuildsContainerJson?.Children[i]);
                            if (jsonAsString.Contains("shareSlug")) break;
                        }
                    }                    
                }

                List<object>? buildWrapper = JsonSerializer.Deserialize<List<object>>(jsonAsString, deserializeOptions);

                // Assume index 3 always contains the build data.
                jsonAsString = JsonSerializer.Serialize(buildWrapper?[3]);
                InfinityBuildsWrapperJson? infinityBuildsWrapperJson = JsonSerializer.Deserialize<InfinityBuildsWrapperJson>(jsonAsString, deserializeOptions);
                if (infinityBuildsWrapperJson == null) return;

                // Valid json - Convert to InfinityBuildsBuild
                InfinityBuildsBuildJson infinityBuildsBuildJson = infinityBuildsWrapperJson.Build;
                InfinityBuildsBuild infinityBuildsBuild = new InfinityBuildsBuild
                {
                    Id = infinityBuildsBuildJson.ShareSlug,
                    Url = _buildUrl,
                    Name = infinityBuildsBuildJson.Title,
                    Date = infinityBuildsBuildJson.UpdatedAt
                };

                WeakReferenceMessenger.Default.Send(new InfinityBuildsStatusUpdateMessage(new InfinityBuildsStatusUpdateMessageParams
                {
                    Build = infinityBuildsBuild,
                    Status = $"Exporting {infinityBuildsBuild.Name}."
                }));

                ExportBuildVariants(infinityBuildsBuild, infinityBuildsBuildJson);
                ConvertBuildVariants(infinityBuildsBuild);

                // Save build
                Directory.CreateDirectory(@".\Builds\InfinityBuilds");
                using (FileStream stream = File.Create(@$".\Builds\InfinityBuilds\{infinityBuildsBuild.Id}.json"))
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    JsonSerializer.Serialize(stream, infinityBuildsBuild, options);
                }
                LoadAvailableInfinityBuildsBuilds();

                WeakReferenceMessenger.Default.Send(new InfinityBuildsStatusUpdateMessage(new InfinityBuildsStatusUpdateMessageParams
                {
                    Build = infinityBuildsBuild,
                    Status = $"Done."
                }));
            }

            FinalizeBuildDownload();
        }

        public void RemoveInfinityBuildsBuild(string buildId)
        {
            try
            {
                string directory = @".\Builds\InfinityBuilds";
                File.Delete(@$"{directory}\{buildId}.json");
                LoadAvailableInfinityBuildsBuilds();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }       

        #endregion
    }
}
