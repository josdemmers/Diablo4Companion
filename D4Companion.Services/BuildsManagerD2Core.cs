using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using D4Companion.Messages;
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
    public class BuildsManagerD2Core : IBuildsManagerD2Core
    {
        private readonly IAffixManager _affixManager;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private string _buildId = string.Empty;
        private string _buildUrl = string.Empty;
        private object _lockTimerTimeout = new();
        private List<D2CoreBuild> _d2CoreBuilds = new();
        private System.Timers.Timer _timerTimeout = new();
        private ChromeDriver? _webDriver = null;
        private DevToolsSession? _devToolsSession = null;
        private WebDriverWait? _webDriverWait = null;
        private int _webDriverProcessId = 0;

        // Start of Constructors region

        #region Constructors

        public BuildsManagerD2Core(ILogger<BuildsManagerD2Core> logger, IAffixManager affixManager, ISettingsManager settingsManager)
        {
            // Init services
            _affixManager = affixManager;
            _logger = logger;
            _settingsManager = settingsManager;

            // Init timers
            _timerTimeout.Interval = 10000;
            _timerTimeout.Elapsed += TimerTimeoutElapsedHandler;

            // Load available D2Core builds
            Task.Factory.StartNew(() =>
            {
                LoadAvailableD2CoreBuilds();
            });
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<D2CoreBuild> D2CoreBuilds { get => _d2CoreBuilds; set => _d2CoreBuilds = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void TimerTimeoutElapsedHandler(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _timerTimeout.Stop();

            WeakReferenceMessenger.Default.Send(new D2CoreStatusUpdateMessage(new D2CoreStatusUpdateMessageParams
            {
                Status = $"Timeout occurred."
            }));

            FinalizeBuildDownload();
        }

        #endregion

        // Start of Methods region

        #region Methods

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
                        
                        if (args.Response.MimeType.Equals("application/json") && args.Response.Url.Contains("web?env=diablocore"))
                        {
                            // function-user-querycurrentuserinfo
                            // Need to create account to test this. Requires mobile number to register.

                            // function-planner-queryplan
                            //{"requestId":"...","data":{"response_data":"{\"data\":{\"_id\":\"1MWy\",\"char\":\"Paladin\",\"title\":\"...

                            // Unknown - possible function-user-querycurrentuserinfo but no account to test with.
                            //{"requestId":"...","data":{"response_data":"{\"data\":null}"}}

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

                            //System.Diagnostics.Debug.WriteLine($"Response body for {args.Response.Url}: {body?.Body}");
                            string json = body?.Body ?? string.Empty;

                            // Only interested in request function-planner-queryplan
                            if (json.Contains($"{{\"response_data\":\"{{\\\"data\\\":{{\\\"_id\\\":\\\"{_buildId}\\\""))
                            {
                                D2CoreBuildJson? d2CoreBuildJson = JsonSerializer.Deserialize<D2CoreBuildJson>(json);
                                if (d2CoreBuildJson != null)
                                {
                                    D2CoreBuildDataJson? d2CoreBuildDataJson = null;
                                    d2CoreBuildDataJson = JsonSerializer.Deserialize<D2CoreBuildDataJson>(d2CoreBuildJson.Data.ResponseData);
                                    if (d2CoreBuildDataJson != null)
                                    {
                                        // Valid json - Save and refresh available builds.
                                        Directory.CreateDirectory(@".\Builds\D2Core");
                                        File.WriteAllText(@$".\Builds\D2Core\{_buildId}.json", d2CoreBuildJson.Data.ResponseData);
                                        LoadAvailableD2CoreBuilds();

                                        FinalizeBuildDownload();
                                    }
                                }
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

        public void CreatePresetFromD2CoreBuild(D2CoreBuild d2CoreBuild, string buildNameOriginal, string buildName)
        {
            buildName = string.IsNullOrWhiteSpace(buildName) ? buildNameOriginal : buildName;

            // Note: Only allow one D2Core build. Update if already exists.
            _affixManager.AffixPresets.RemoveAll(p => p.Name.Equals(buildName));

            var affixPreset = new AffixPreset
            {
                Name = buildName
            };

            var d2CoreBuildDataVariantJson = d2CoreBuild.Data.Variants.FirstOrDefault(v => v.Name.Equals(buildNameOriginal));
            if (d2CoreBuildDataVariantJson == null) return;

            // Loop through all items
            List<string> aspects = [];
            string itemType = string.Empty;
            foreach (var item in d2CoreBuildDataVariantJson.Gear)
            {
                switch (item.Value.ItemType)
                {
                    case "Helm":
                        itemType = Constants.ItemTypeConstants.Helm;
                        break;
                    case "ChestArmor":
                        itemType = Constants.ItemTypeConstants.Chest;
                        break;
                    case "DruidOffhand":
                    case "Focus":
                    case "Shield":
                        itemType = Constants.ItemTypeConstants.Offhand;
                        break;
                    case "Dagger":
                    case "Glaive":
                    case "Mace":
                    case "Mace2H":
                    case "Polearm":
                    case "Quarterstaff":
                    case "Scythe":
                    case "Scythe2H":
                    case "Staff":
                    case "Sword":
                    case "Sword2H":
                    case "Wand":
                        itemType = Constants.ItemTypeConstants.Weapon;
                        break;
                    case "Bow":
                    case "Crossbow2H":
                        itemType = Constants.ItemTypeConstants.Ranged;
                        break;
                    case "Gloves":
                        itemType = Constants.ItemTypeConstants.Gloves;
                        break;
                    case "Legs":
                        itemType = Constants.ItemTypeConstants.Pants;
                        break;
                    case "Boots":
                        itemType = Constants.ItemTypeConstants.Boots;
                        break;
                    case "Ring":
                        itemType = Constants.ItemTypeConstants.Ring;
                        break;
                    case "Amulet":
                        itemType = Constants.ItemTypeConstants.Amulet;
                        break;
                    default:
                        _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown itemtype id: {item.Value.ItemType}");
                        WeakReferenceMessenger.Default.Send(new WarningOccurredMessage(new WarningOccurredMessageParams
                        {
                            Message = $"Imported D2Core build contains unknown itemtype id: {item.Value.ItemType}."
                        }));
                        continue;
                }

                // Process runes
                foreach (var socket in item.Value.Sockets)
                {
                    if (!socket.Type.Equals("rune")) continue;

                    if (!affixPreset.ItemRunes.Any(r => r.Id.Equals($"Item_{socket.Key}")))
                    {
                        affixPreset.ItemRunes.Add(new ItemAffix { Id = $"Item_{socket.Key}", Type = Constants.ItemTypeConstants.Rune });
                    }
                }

                // Process unique items
                if (item.Value.Type.Equals("uniqueItem"))
                {
                    string uniqueId = item.Value.Key;
                    var uniqueInfo = _affixManager.Uniques.FirstOrDefault(u => u.IdNameItem.Contains(uniqueId)) ??
                        _affixManager.Uniques.FirstOrDefault(u => u.IdNameItemActor.Equals(uniqueId));

                    if (uniqueInfo != null)
                    {
                        // Add unique items
                        affixPreset.ItemUniques.Add(new ItemAffix
                        {
                            Id = uniqueInfo.IdName,
                            Type = string.Empty,
                            Color = _settingsManager.Settings.DefaultColorUniques
                        });

                        // Skip unique affixes
                        if (!_settingsManager.Settings.IsImportUniqueAffixesD2CoreEnabled) continue;
                    }
                }

                // Process implcit affixes
                // Implicit affixes - currently not supported by D2Core.com

                // Process explicit affixes
                foreach (var affix in item.Value.Mods)
                {
                    AffixInfo? affixInfo = _affixManager.GetAffixInfoByIdName(affix.Name);

                    if (affixInfo == null)
                    {
                        _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown affix: {affix.Name}");
                        WeakReferenceMessenger.Default.Send(new WarningOccurredMessage(new WarningOccurredMessageParams
                        {
                            Message = $"Imported D2Core build contains unknown affix: {affix.Name}."
                        }));
                    }
                    else
                    {
                        if (!affixPreset.ItemAffixes.Any(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType) && !a.IsImplicit))
                        {
                            affixPreset.ItemAffixes.Add(new ItemAffix
                            {
                                Id = affixInfo.IdName,
                                Type = itemType,
                                Color = affix.Greater ? _settingsManager.Settings.DefaultColorGreater : _settingsManager.Settings.DefaultColorNormal,
                                IsGreater = affix.Greater,
                                IsTempered = affix.Name.StartsWith("Tempered_",StringComparison.OrdinalIgnoreCase)
                            });
                        }
                    }
                }

                // Process aspect / legendary power
                if (item.Value.Type.Equals("legendary"))
                {
                    aspects.Add(item.Value.Key);
                }
            }

            // Add all aspects / legendary powers
            foreach (var aspect in aspects)
            {
                var aspectId = aspect.Replace("Affix_", string.Empty, StringComparison.OrdinalIgnoreCase);

                if (!affixPreset.ItemAspects.Any(a => a.Id.Equals(aspectId)))
                {
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId!, Type = Constants.ItemTypeConstants.Helm });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId!, Type = Constants.ItemTypeConstants.Chest });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId!, Type = Constants.ItemTypeConstants.Gloves });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId!, Type = Constants.ItemTypeConstants.Pants });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId!, Type = Constants.ItemTypeConstants.Boots });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId!, Type = Constants.ItemTypeConstants.Amulet });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId!, Type = Constants.ItemTypeConstants.Ring });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId!, Type = Constants.ItemTypeConstants.Weapon });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId!, Type = Constants.ItemTypeConstants.Ranged });
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId!, Type = Constants.ItemTypeConstants.Offhand });
                }
            }

            // Process paragon boards
            if (_settingsManager.Settings.IsImportParagonD2CoreEnabled)
            {
                var paragonBoards = new List<ParagonBoard>();
                foreach (var paragonBoardData in d2CoreBuildDataVariantJson.Paragon)
                {
                    var paragonBoard = new ParagonBoard();
                    string name = paragonBoardData.Key;
                    string glyph = paragonBoardData.Value.Glyph.First().Value;

                    paragonBoard.Name = _affixManager.GetParagonBoardLocalisation(name);
                    paragonBoard.Glyph = _affixManager.GetParagonGlyphLocalisationByNumber(glyph);
                    string rotationInfo = paragonBoardData.Value.Rotate == 0 ? "0°" :
                        paragonBoardData.Value.Rotate == 1 ? "90°" :
                        paragonBoardData.Value.Rotate == 2 ? "180°" :
                        paragonBoardData.Value.Rotate == 3 ? "270°" : "?°";
                    paragonBoard.Rotation = rotationInfo;
                    paragonBoard.Index = paragonBoardData.Value.Index;
                    paragonBoards.Add(paragonBoard);

                    // Process nodes
                    int rotation = paragonBoardData.Value.Rotate;
                    foreach (var locationString in paragonBoardData.Value.Data)
                    {
                        int x = int.Parse(locationString.Split('_')[1]);
                        int y = int.Parse(locationString.Split('_')[0]);
                        int location = x + y * 21;

                        int locationT = location;
                        int locationX = location % 21;
                        int locationY = location / 21;
                        int locationXT = locationX;
                        int locationYT = locationY;
                        switch (rotation)
                        {
                            case 0:
                                locationT = location;
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
                                locationT = location;
                                break;
                        }
                        paragonBoard.Nodes[locationT] = true;
                    }
                }

                // Sort paragon boards by index.
                paragonBoards.Sort((x, y) =>
                {
                    return x.Index.CompareTo(y.Index);
                });

                affixPreset.ParagonBoardsList.Add(paragonBoards);
            }
            _affixManager.AddAffixPreset(affixPreset);
        }

        public void DownloadD2CoreBuild(string buildId)
        {
            string buildUrl = string.Empty;

            try
            {
                WeakReferenceMessenger.Default.Send(new D2CoreStatusUpdateMessage(new D2CoreStatusUpdateMessageParams
                {
                    Status = $"Preparing browser instance."
                }));

                buildUrl = $"https://www.d2core.com/d4/planner?bd={buildId}";
                _buildId = buildId;
                _buildUrl = buildUrl;

                if (_webDriver == null) InitSelenium();
                if (_webDriver == null) throw new Exception("WebDriver initialization failed.");
                if (_webDriverWait == null) throw new Exception("WebDriverWait initialization failed.");
                if (_devToolsSession == null) throw new Exception("DevToolsSession initialization failed.");

                WeakReferenceMessenger.Default.Send(new D2CoreStatusUpdateMessage(new D2CoreStatusUpdateMessageParams
                {
                    Status = $"Downloading {buildUrl}."
                }));
                _webDriver.Navigate().GoToUrl(buildUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{MethodBase.GetCurrentMethod()?.Name} ({buildUrl})");

                WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                {
                    Message = $"Failed to download from D2Core ({buildUrl})"
                }));

                WeakReferenceMessenger.Default.Send(new D2CoreStatusUpdateMessage(new D2CoreStatusUpdateMessageParams
                {
                    Status = $"Failed. See log."
                }));

                FinalizeBuildDownload();
            }
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

            WeakReferenceMessenger.Default.Send(new D2CoreCompletedMessage());
        }

        private void LoadAvailableD2CoreBuilds()
        {
            try
            {
                D2CoreBuilds.Clear();

                string directory = @".\Builds\D2Core";
                if (Directory.Exists(directory))
                {
                    var fileEntries = Directory.EnumerateFiles(directory).Where(tooltip => tooltip.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                    foreach (string fileName in fileEntries)
                    {
                        string json = File.ReadAllText(fileName);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            // create the options
                            var options = new JsonSerializerOptions()
                            {
                                WriteIndented = true
                            };
                            // register the converter
                            options.Converters.Add(new StringConverter());

                            D2CoreBuildDataJson? d2CoreBuildDataJson = JsonSerializer.Deserialize<D2CoreBuildDataRootJson>(json, options)?.Data;
                            if (d2CoreBuildDataJson != null)
                            {
                                D2CoreBuild d2CoreBuild = new D2CoreBuild
                                {
                                    Data = d2CoreBuildDataJson,
                                    Date = DateTimeOffset.FromUnixTimeMilliseconds(d2CoreBuildDataJson.UpdateTime).LocalDateTime.ToString(),
                                    Id = d2CoreBuildDataJson.Id,
                                    Name = d2CoreBuildDataJson.Title
                                };

                                D2CoreBuilds.Add(d2CoreBuild);
                            }
                        }
                    }

                    // Set empty varient name to build name
                    foreach (var build in D2CoreBuilds)
                    {
                        foreach (var variant in build.Data.Variants)
                        {
                            if (string.IsNullOrWhiteSpace(variant.Name))
                            {
                                variant.Name = build.Name;
                            }
                        }
                    }

                    WeakReferenceMessenger.Default.Send(new D2CoreBuildsLoadedMessage());
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        public void RemoveD2CoreBuild(string buildId)
        {
            try
            {
                string directory = @".\Builds\D2Core";
                File.Delete(@$"{directory}\{buildId}.json");
                LoadAvailableD2CoreBuilds();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion
    }
}
