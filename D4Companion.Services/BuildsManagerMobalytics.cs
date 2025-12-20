using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using FuzzierSharp;
using FuzzierSharp.SimilarityRatio;
using FuzzierSharp.SimilarityRatio.Scorer.Composite;
using FuzzierSharp.SimilarityRatio.Scorer.StrategySensitive;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Support.UI;
using Prism.Events;
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
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
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

        public BuildsManagerMobalytics(IEventAggregator eventAggregator, ILogger<BuildsManagerMobalytics> logger, IAffixManager affixManager, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            // Init logger
            _logger = logger;

            // Init services
            _affixManager = affixManager;
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

            _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Status = $"Timeout occurred." });

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

            _devToolsSession = _webDriver.GetDevToolsSession();

            // Tweak settings when handling bigger json responses
            var enableCommandSettingsType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "EnableCommandSettings");
            var enableCommandSettings = Activator.CreateInstance(enableCommandSettingsType);
            //enableCommandSettingsType.GetProperty("MaxPostDataSize")?.SetValue(enableCommandSettings, (long?)(20 * 1024 * 1024));       // 20 MB post data
            //enableCommandSettingsType.GetProperty("MaxResourceBufferSize")?.SetValue(enableCommandSettings, (long?)(20 * 1024 * 1024)); // 20 MB per resource
            //enableCommandSettingsType.GetProperty("MaxTotalBufferSize")?.SetValue(enableCommandSettings, (long?)(200 * 1024 * 1024));   // 200 MB total buffer

            var setCacheDisabledCommandSettingsType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "SetCacheDisabledCommandSettings");
            var setCacheDisabledCommandSettings = Activator.CreateInstance(setCacheDisabledCommandSettingsType);
            setCacheDisabledCommandSettingsType.GetProperty("CacheDisabled")?.SetValue(setCacheDisabledCommandSettings, true);

            var clearBrowserCacheCommandSettingsType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "ClearBrowserCacheCommandSettings");
            var clearBrowserCacheCommandSettings = Activator.CreateInstance(clearBrowserCacheCommandSettingsType);
            var clearBrowserCookiesCommandSettingsType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "ClearBrowserCookiesCommandSettings");
            var clearBrowserCookiesCommandSettings = Activator.CreateInstance(clearBrowserCookiesCommandSettingsType);

            var networkAdapterType = DevToolsHelper.GetTypeFromNetworkNamespaceByName(_devToolsSession, "NetworkAdapter");
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
                            var getResponseBodyCommandSettings = Activator.CreateInstance(getResponseBodyCommandSettingsType);
                            getResponseBodyCommandSettingsType.GetProperty("RequestId")?.SetValue(getResponseBodyCommandSettings, args.RequestId);
                            // Call GetResponseBody
                            var task = (Task?)getResponseBodyMethod?.Invoke(networkAdapter, new[] { getResponseBodyCommandSettings, CancellationToken.None, null, true });
                            task?.Wait();
                            var resultProperty = task?.GetType().GetProperty("Result");
                            dynamic? body = resultProperty?.GetValue(task);

                            //System.Diagnostics.Debug.WriteLine($"Response body for {args.Response.Url}: {body?.Body}");
                            string json = body?.Body ?? string.Empty;

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

        /// <summary>
        /// Version specific DevTools initialization for v142.
        /// Use reflection based InitDevTools() instead.
        /// </summary>
        private async void InitDevTools142()
        {
            if (_webDriver == null) return;

            _devToolsSession = _webDriver.GetDevToolsSession();

            var domains = _devToolsSession.GetVersionSpecificDomains<OpenQA.Selenium.DevTools.V142.DevToolsSessionDomains>();

            // Create the settings objects
            var enableSettings = new OpenQA.Selenium.DevTools.V142.Network.EnableCommandSettings
            {
                // Tweak settings when handling bigger json responses
                //MaxPostDataSize = 20 * 1024 * 1024, // 20 MB post data
                //MaxResourceBufferSize = 20 * 1024 * 1024, // 20 MB per resource
                //MaxTotalBufferSize = 200 * 1024 * 1024,  // 200 MB total buffer
            };
            var cacheDisabledSettings = new OpenQA.Selenium.DevTools.V142.Network.SetCacheDisabledCommandSettings
            {
                CacheDisabled = true
            };

            // Enable network domain
            await domains.Network.Enable(enableSettings);
            // Clear + disable cache
            await domains.Network.ClearBrowserCache();
            await domains.Network.ClearBrowserCookies();
            await domains.Network.SetCacheDisabled(cacheDisabledSettings);

            domains.Network.ResponseReceived += async (sender, e) =>
            {
                Thread.Sleep(2500); // Give some time for the response body to be ready.
                if (e.Response.MimeType.Equals("application/json") && e.Response.Url.Contains("api/diablo4"))
                {
                    var body = await domains.Network.GetResponseBody(new OpenQA.Selenium.DevTools.V142.Network.GetResponseBodyCommandSettings
                    {
                        RequestId = e.RequestId
                    });

                    System.Diagnostics.Debug.WriteLine($"Response body for {e.Response.Url}: {body.Body}");
                }
            };
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

            // Cache related settings
            options.AddArgument("--disable-cache");
            options.AddArgument("--disk-cache-size=0");
            options.AddArgument("--media-cache-size=0");

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

        private string CleanAffixText(string affixText)
        {
            string affixTextClean = affixText;

            List<string> implicitPrefixes = new List<string>
            {
                "1h-axe-",
                "1h-mace-",
                "1h-scythe-",
                "1h-sword-",
                "2h-axe-",
                "2h-mace-",
                "2h-scythe-",
                "2h-sword-",
                "bow-",
                "crossbow-",
                "dagger-",
                "glaive-",
                "offhand-",
                "polearm-",
                "quarterstaff-",
                "shield-",
                "staff-",
                "wand-"
            };

            List<string> temperingPrefixes = new List<string>
            {
                "agile-augments-",
                "agility-efficiency-",
                "alchemist-control-",
                "arsenal-finesse-",
                "assassin-augments-",
                "aura-innovation-",
                "barbarian-breach-",
                "barbarian-control-",
                "barbarian-innovation-",
                "barbarian-motion-",
                "barbarian-protection-",
                "barbarian-recovery-",
                "barbarian-strategy-",
                "basic-augments-rogue-",
                "berserking-augments-",
                "berserking-finesse-",
                "bleed-augments-",
                "bleed-innovation-",
                "blood-augments-",
                "blood-endurance-",
                "blood-finesse-",
                "blood-innovation-",
                "bone-augments-",
                "bone-finesse-",
                "bone-innovation-",
                "brawling-augments-",
                "brawling-efficiency-",
                "brute-innovation-",
                "centipede-augments-",
                "centipede-efficiency-",
                "centipede-finesse-",
                "centipede-innovation-",
                "companion-augments-",
                "companion-efficiency-",
                "companion-finesse-",
                "companion-innovation-",
                "conjuration-augments-",
                "conjuration-efficiency-",
                "conjuration-finesse-",
                "conjuration-fortune-",
                "core-augments-barbarian-",
                "core-augments-rogue-",
                "cutthroat-augments-",
                "cutthroat-finesse-",
                "daze-control-",
                "decay-innovation-",
                "demolition-finesse-",
                "disciple-augments-",
                "disciple-efficiency-",
                "disciple-innovation-",
                "dreadful-augments-",
                "druid-invigoration-",
                "druid-motion-",
                "eagle-augments-",
                "eagle-efficiency-",
                "eagle-finesse-",
                "eagle-innovation-",
                "earth-augments-",
                "earth-finesse-",
                "elemental-control-",
                "elemental-finesse-day-",
                "elemental-finesse-night-",
                "elemental-surge-day-",
                "elemental-surge-night-",
                "elemental-surge-", // Handle as last
                "execution-innovation-",
                "fitness-efficiency-",
                "forest-augments-",
                "frost-augments-",
                "frost-cage-",
                "frost-finesse-",
                "furious-augments-",
                "gorilla-augments-",
                "gorilla-efficiency-",
                "gorilla-finesse-",
                "gorilla-innovation-",
                "imbuement-abundance-",
                "jaguar-augments-",
                "jaguar-efficiency-",
                "jaguar-finesse-",
                "jaguar-innovation-",
                "judicator-augments-",
                "judicator-efficiency-",
                "judicator-innovation-",
                "juggernaut-augments-",
                "juggernaut-efficiency-",
                "juggernaut-innovation-",
                "lightning-augments-",
                "marksman-augments-",
                "marksman-finesse-",
                "martial-finesse-",
                "minion-augments-",
                "minion-finesse-",
                "mystical-augments-",
                "natural-finesse-",
                "natural-motion-",
                "natural-resistance-",
                "natural-schemes-",
                "nature-magic-innovation-",
                "nature-magic-wall-",
                "necromancer-efficiency-",
                "necromancer-invigoration-",
                "necromancer-motion-",
                "necromancer-wall-",
                "paladin-guard-",
                "paladin-motion-",
                "paladin-perseverance-",
                "paladin-recovery-",
                "paladin-resolve-",
                "plains-augments-",
                "prismatic-augments-",
                "profane-cage-",
                "profane-finesse-",
                "profane-innovation-",
                "pyromancy-augments-",
                "pyromancy-endurance-",
                "pyromancy-finesse-",
                "pyromancy-innovation-",
                "rogue-cloaking-",
                "rogue-innovation-",
                "rogue-invigoration-",
                "rogue-motion-",
                "rogue-persistence-",
                "rogue-recovery-",
                "sandstorm-augments-",
                "scoundrel-finesse-",
                "shadow-augments-",
                "shadow-finesse-",
                "shapeshifting-endurance-",
                "shapeshifting-finesse-",
                "shock-augments-",
                "shock-finesse-",
                "skillful-finesse-",
                "sky-augments-",
                "slayers-finesse-",
                "soil-augments-",
                "sorcerer-control-",
                "sorcerer-innovation-",
                "sorcerer-motion-",
                "sorcerer-stability-",
                "specialist-evolution-",
                "spiritborn-endurance-",
                "spiritborn-guard-",
                "spiritborn-motion-",
                "spiritborn-recovery-",
                "spiritborn-resolve-",
                "storm-augments-",
                "storm-finesse-",
                "subterfuge-efficiency-",
                "subterfuge-expertise-",
                "summoning-augments-",
                "summoning-finesse-",
                "thorn-army-",
                "thorn-body-",
                "trap-augments-",
                "trap-expertise-",
                "trickster-finesse-",
                "ultimate-efficiency-barbarian-",
                "ultimate-efficiency-druid-",
                "ultimate-efficiency-sorcerer-",
                "ultimate-efficiency-", // Handle as last
                "vehement-augments-",
                "warped-augments-",
                "wasteland-augments-",
                "wasteland-innovation-",
                "weapon-attunement-barbarian-",
                "weapon-attunement-necromancer-",
                "weapon-augments-",
                "weapon-mastery-efficiency-",
                "werebear-augments-",
                "werebear-innovation-",
                "werewolf-augments-",
                "werewolf-finesse-",
                "wordly-endurance-",
                "wordly-fortune-",
                "wordly-stability-",
                "worldly-endurance-",
                "worldly-finesse-",
                "worldly-fortune-",
                "worldly-stability-",
                "worldy-finesse-",
                "wrath-efficiency-",
                "zealot-augments-",
                "zealot-efficiency-",
                "zealot-finesse-",
                "zealot-innovation-"
            };

            List<string> allPrefixes = implicitPrefixes.Concat(temperingPrefixes).ToList();
            foreach (string prefix in allPrefixes)
            {
                // Remove prefixes
                affixTextClean = affixTextClean.Replace(prefix, string.Empty);
            }

            // Remove hyphens and numbers
            affixTextClean = affixTextClean.Replace("-", " ");
            affixTextClean = System.Text.RegularExpressions.Regex.Replace(affixTextClean, @"\d", "");

            return affixTextClean.Trim();
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
                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Status = $"Preparing browser instance." });

                _buildUrl = buildUrl;

                if (_webDriver == null) InitSelenium();
                if (_webDriver == null) throw new Exception("WebDriver initialization failed.");
                if (_webDriverWait == null) throw new Exception("WebDriverWait initialization failed.");

                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Status = $"Downloading {buildUrl}." });
                _webDriver.Navigate().GoToUrl(buildUrl);

                // For profile page use javascript to extract data.
                if (buildUrl.Contains("/profile/") && !buildUrl.Contains("/builds/"))
                {
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
                            ParseJsonProfile(jsonString);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{MethodBase.GetCurrentMethod()?.Name} ({buildUrl})");

                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"Failed to download from Mobalytics ({buildUrl})"
                });

                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Status = $"Failed." });
            }
        }

        private void ConvertBuildVariants(MobalyticsBuild mobalyticsBuild)
        {
            foreach (var variant in mobalyticsBuild.Variants)
            {
                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Converting {variant.Name}." });

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
                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Converted {variant.Name}." });
            }
        }

        private ItemAffix ConvertItemAffix(Tuple<string, MobalyticsAffix> affixDescription)
        {
            string affixId = string.Empty;
            string itemType = affixDescription.Item1;
            MobalyticsAffix mobalyticsAffix = affixDescription.Item2;

            var result = Process.ExtractOne(mobalyticsAffix.AffixTextClean, _affixDescriptions, scorer: ScorerCache.Get<DefaultRatioScorer>());
            affixId = _affixMapDescriptionToId[result.Value];

            Color color = mobalyticsAffix.IsImplicit ? _settingsManager.Settings.DefaultColorImplicit :
                mobalyticsAffix.IsGreater ? _settingsManager.Settings.DefaultColorGreater :
                mobalyticsAffix.IsTempered ? _settingsManager.Settings.DefaultColorTempered :
                _settingsManager.Settings.DefaultColorNormal;
            return new ItemAffix
            {
                Id = affixId,
                Type = itemType,
                Color = color,
                IsGreater = mobalyticsAffix.IsGreater,
                IsImplicit = mobalyticsAffix.IsImplicit,
                IsTempered = mobalyticsAffix.IsTempered
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
            _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Exporting {variantName}." });

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
            _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Exported {variantName}." });
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

            _eventAggregator.GetEvent<MobalyticsCompletedEvent>().Publish();
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
                if (isUniqueItem && !_settingsManager.Settings.IsImportUniqueAffixesMobalyticsEnabled) return affixes;

                // Explicit
                foreach (var affix in itemSlot.GameEntity.Modifiers?.GearStats ?? Enumerable.Empty<MobalyticsBuildModifiersGearStatJson>())
                {
                    if (affix == null) continue;

                    MobalyticsAffix mobalyticsAffix = new MobalyticsAffix();
                    mobalyticsAffix.IsGreater = affix.IsGreater;
                    mobalyticsAffix.IsImplicit = false;
                    mobalyticsAffix.IsTempered = false;
                    mobalyticsAffix.AffixText = affix.Id;
                    mobalyticsAffix.AffixTextClean = CleanAffixText(mobalyticsAffix.AffixText);
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
                    mobalyticsAffix.AffixTextClean = CleanAffixText(mobalyticsAffix.AffixText);
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
                    mobalyticsAffix.AffixTextClean = CleanAffixText(mobalyticsAffix.AffixText);
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
                // Fix naming inconsistency
                paragonBoard.Name = paragonBoard.Name.Replace("barbarian-starter-board", "barbarian-starting-board");
                paragonBoard.Name = paragonBoard.Name.Replace("druid-starter-board", "druid-starting-board");
                paragonBoard.Name = paragonBoard.Name.Replace("necromancer-starter-board", "necromancer-starting-board");
                paragonBoard.Name = paragonBoard.Name.Replace("paladin-starter-board", "paladin-starting-board");
                paragonBoard.Name = paragonBoard.Name.Replace("rogue-starter-board", "rogue-starting-board");
                paragonBoard.Name = paragonBoard.Name.Replace("sorcerer-starter-board", "sorcerer-starting-board");
                paragonBoard.Name = paragonBoard.Name.Replace("spiritborn-starter-board", "spiritborn-starting-board");
                paragonBoard.Glyph = board.Glyph?.Slug ?? string.Empty;
                paragonBoard.Rotation = board.Rotation == 0 ? "0°" :
                                        board.Rotation == 90 ? "90°" :
                                        board.Rotation == 180 ? "180°" :
                                        board.Rotation == 270 ? "270°" : "?°";

                var boardNodes = buildVariant.Paragon.Nodes.Where(n => n.Slug.StartsWith(paragonBoard.Name))?.ToList() ?? 
                    Enumerable.Empty<MobalyticsBuildParagonNodeJson>().ToList();

                if (boardNodes.Count == 0)
                {
                    _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                    {
                        Message = $"No nodes found for paragon board {paragonBoard.Name}."
                    });
                }

                foreach (var node in boardNodes)
                {
                    string nodePosition = node.Slug.Replace(paragonBoard.Name + "-", string.Empty);

                    int locationX = int.Parse(nodePosition.Split("-")[0].Substring(1));
                    int locationY = int.Parse(nodePosition.Split("-")[1].Substring(1));
                    int locationXT = locationX;
                    int locationYT = locationY;

                    if (board.Rotation == 0 ||
                        board.Rotation == 360)
                    {
                        locationXT = locationXT - 1;
                        locationYT = locationYT - 1;
                    }
                    else if (board.Rotation == 90)
                    {
                        locationXT = 21 - locationY;
                        locationYT = locationX;
                        locationYT = locationYT - 1;
                    }
                    else if (board.Rotation == 180)
                    {
                        locationXT = 21 - locationX;
                        locationYT = 21 - locationY;
                    }
                    else if (board.Rotation == 270)
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

                    _eventAggregator.GetEvent<MobalyticsBuildsLoadedEvent>().Publish();
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

                    _eventAggregator.GetEvent<MobalyticsProfilesLoadedEvent>().Publish();
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

                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Exporting {mobalyticsBuild.Name}." });

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

                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Done." });
            }

            FinalizeBuildDownload();
        }

        private void ParseJsonProfile(string json)
        {
            MobalyticsProfileJson? mobalyticsProfileJson = JsonSerializer.Deserialize<MobalyticsProfileJson>(json);
            if (mobalyticsProfileJson != null)
            {
                // Valid json - Convert to MobalyticsProfile
                var author = mobalyticsProfileJson.Apollo.Graphql.FirstOrDefault(g => g.Key.StartsWith("NgfDocumentAuthor:"));
                string authorJsonString = JsonSerializer.Serialize(author.Value);
                var ngfDocumentAuthorJson = JsonSerializer.Deserialize<MobalyticsProfileNgfDocumentAuthorJson>(authorJsonString);

                if (ngfDocumentAuthorJson != null)
                {
                    // Valid json - Convert to NgfDocumentAuthor
                    string profileId = ngfDocumentAuthorJson.Id;
                    string profileName = ngfDocumentAuthorJson.Name;

                    MobalyticsProfile mobalyticsProfile = new MobalyticsProfile
                    {
                        Id = profileId,
                        Name = profileName
                    };

                    _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Profile = mobalyticsProfile, Status = $"Exporting {mobalyticsProfile.Name}." });

                    var builds = mobalyticsProfileJson.Apollo.Graphql
                        .Where(g => g.Key.StartsWith("Diablo4UserGeneratedDocument:"))
                        .Select(g => JsonSerializer.Deserialize<MobalyticsProfileDiablo4UserGeneratedDocumentJson>(JsonSerializer.Serialize(g.Value))).ToList();

                    foreach (var build in builds)
                    {
                        if (build == null) continue;

                        var mobalyticsBuildVariant = new MobalyticsProfileBuildVariant
                        {
                            Date = build.UpdatedAt,
                            Id = build.Id,
                            Name = build.Data.Name,
                            Url = $"{mobalyticsProfile.Url}/builds/{build.Id}"
                        };

                        mobalyticsProfile.Variants.Add(mobalyticsBuildVariant);
                        _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Profile = mobalyticsProfile, Status = $"Exported {build.Data.Name}." });
                    }

                    // Save build
                    Directory.CreateDirectory(@".\Profiles\Mobalytics");
                    using (FileStream stream = File.Create(@$".\Profiles\Mobalytics\{mobalyticsProfile.Id}.json"))
                    {
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        JsonSerializer.Serialize(stream, mobalyticsProfile, options);
                    }
                    LoadAvailableMobalyticsProfiles();

                    _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Profile = mobalyticsProfile, Status = $"Done." });
                }
                else
                {
                    _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Status = $"Failed. Invalid json." });
                }
            }
            else
            {
                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Status = $"Failed. Invalid json." });
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

        public void RemoveMobalyticsProfile(string profileId)
        {
            try
            {
                string directory = @".\Profiles\Mobalytics";
                File.Delete(@$"{directory}\{profileId}.json");
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
