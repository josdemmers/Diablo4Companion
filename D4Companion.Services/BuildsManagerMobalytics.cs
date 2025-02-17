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

namespace D4Companion.Services
{
    public class BuildsManagerMobalytics : IBuildsManagerMobalytics
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly ISettingsManager _settingsManager;

        private static readonly int _delayClick = 500;
        private static readonly int _delayClickParagon = 2000;

        private List<AffixInfo> _affixes = new List<AffixInfo>();
        private List<string> _affixDescriptions = new List<string>();
        private Dictionary<string, string> _affixMapDescriptionToId = new Dictionary<string, string>();
        private List<AspectInfo> _aspects = new List<AspectInfo>();
        private List<string> _aspectNames = new List<string>();
        private Dictionary<string, string> _aspectMapNameToId = new Dictionary<string, string>();
        private List<MobalyticsBuild> _mobalyticsBuilds = new();
        private List<RuneInfo> _runes = new List<RuneInfo>();
        private List<string> _runeNames = new List<string>();
        private Dictionary<string, string> _runeMapNameToId = new Dictionary<string, string>();
        private List<UniqueInfo> _uniques = new List<UniqueInfo>();
        private List<string> _uniqueNames = new List<string>();
        private Dictionary<string, string> _uniqueMapNameToId = new Dictionary<string, string>();
        private WebDriver? _webDriver = null;
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

            // Load available Mobalytics builds.
            Task.Factory.StartNew(() =>
            {
                LoadAvailableMobalyticsBuilds();
            });
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<MobalyticsBuild> MobalyticsBuilds { get => _mobalyticsBuilds; set => _mobalyticsBuilds = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

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

            // Service
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            // Create driver
            _webDriver = new ChromeDriver(service: service, options: options);
            _webDriverWait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10));
            _webDriverProcessId = service.ProcessId;
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
            string id = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(buildUrl));
            id = id.Length > 100 ? id.Substring(id.Length - 100) : id;
            id = id.Replace("/", string.Empty);

            try
            {
                if (_webDriver == null) InitSelenium();

                MobalyticsBuild mobalyticsBuild = new MobalyticsBuild
                {
                    Id = id,
                    Url = buildUrl
                };

                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Downloading {mobalyticsBuild.Url}." });
                _webDriver.Navigate().GoToUrl(mobalyticsBuild.Url);

                try
                {
                    // Wait for cookies
                    var elementCookie = _webDriverWait.Until(e =>
                    {
                        var elements = _webDriver.FindElements(By.ClassName("qc-cmp2-summary-buttons"));
                        if (elements.Count > 0 && elements[0].Displayed)
                        {
                            return elements[0];
                        }
                        return null;
                    });

                    // Accept cookies
                    if (elementCookie != null)
                    {
                        //var asHtml = elementCookie.GetAttribute("innerHTML");
                        elementCookie.FindElements(By.TagName("button"))[1].Click();
                        Thread.Sleep(_delayClick);
                    }
                }
                catch (Exception)
                {
                    // No cookies when using "options.AddArgument("--headless");"
                }

                // Build name
                mobalyticsBuild.Name = GetBuildName();

                if (string.IsNullOrWhiteSpace(mobalyticsBuild.Name))
                {
                    _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = new MobalyticsBuild { Id = id, Url = buildUrl }, Status = $"Failed - Build name not found." });
                }
                else
                {
                    _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Downloaded {mobalyticsBuild.Name}." });

                    // Last update
                    mobalyticsBuild.Date = GetLastUpdateInfo();

                    // Variants
                    ExportBuildVariants(mobalyticsBuild);
                    ConvertBuildVariants(mobalyticsBuild);

                    // Save
                    Directory.CreateDirectory(@".\Builds\Mobalytics");
                    using (FileStream stream = File.Create(@$".\Builds\Mobalytics\{mobalyticsBuild.Id}.json"))
                    {
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        JsonSerializer.Serialize(stream, mobalyticsBuild, options);
                    }
                    LoadAvailableMobalyticsBuilds();

                    _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Done." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{MethodBase.GetCurrentMethod()?.Name} ({buildUrl})");

                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"Failed to download from Mobalytics ({buildUrl})"
                });

                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = new MobalyticsBuild { Id = id, Url = buildUrl }, Status = $"Failed." });
            }
            finally
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

                _eventAggregator.GetEvent<MobalyticsCompletedEvent>().Publish();
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

            var result = Process.ExtractOne(mobalyticsAffix.AffixText, _affixDescriptions, scorer: ScorerCache.Get<DefaultRatioScorer>());
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

        private void ExportBuildVariants(MobalyticsBuild mobalyticsBuild)
        {
            var elementMain = _webDriver.FindElement(By.TagName("main"));
            var elementMainContent = elementMain.FindElements(By.XPath("./div/div/div[1]/div"));
            var elementVariants = elementMainContent.FirstOrDefault(e =>
            {
                int count = e.FindElements(By.XPath("./div")).Count();
                if (count <= 1) return false;
                int countSpan = e.FindElements(By.XPath("./div[./span]")).Count();

                return count == countSpan;
            });

            // Website layout check - Single or multiple build layout.
            if (elementVariants == null)
            {
                ExportBuildVariant(mobalyticsBuild.Name, mobalyticsBuild);
            }
            else
            {
                var variants = elementVariants.FindElements(By.XPath("./div"));
                //var variantsAsHtml = elementVariants.FindElements(By.XPath("./div")).GetAttribute("innerHTML");
                foreach (var variant in variants)
                {
                    _ = _webDriver?.ExecuteScript("arguments[0].click();", variant);
                    Thread.Sleep(_delayClick);
                    ExportBuildVariant(variant.Text, mobalyticsBuild);
                }
            }
        }

        private void ExportBuildVariant(string variantName, MobalyticsBuild mobalyticsBuild)
        {
            // Set timeout to improve performance
            // https://stackoverflow.com/questions/16075997/iselementpresent-is-very-slow-in-case-if-element-does-not-exist
            _webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(0);

            _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Exporting {variantName}." });

            var mobalyticsBuildVariant = new MobalyticsBuildVariant
            {
                Name = variantName
            };

            // Look for aspect and gear stats container
            // "Aspects & Uniques"
            // "Gear Stats"
            string header = "Aspects & Uniques";
            var aspectAndGearStatsHeader = _webDriver.FindElement(By.XPath($"//header[./div[contains(text(), '{header}')]]")).FindElements(By.TagName("div"));

            // Aspects & Uniques
            _ = _webDriver?.ExecuteScript("arguments[0].click();", aspectAndGearStatsHeader[0]);
            Thread.Sleep(_delayClick);
            mobalyticsBuildVariant.Aspect = GetAllAspects();
            mobalyticsBuildVariant.Uniques = GetAllUniques();

            // Gear Stats
            _ = _webDriver?.ExecuteScript("arguments[0].click();", aspectAndGearStatsHeader[1]);
            Thread.Sleep(_delayClick);

            // Armor
            mobalyticsBuildVariant.Helm = GetAllAffixes("Helm");
            mobalyticsBuildVariant.Chest = GetAllAffixes("Chest Armor");
            mobalyticsBuildVariant.Gloves = GetAllAffixes("Gloves");
            mobalyticsBuildVariant.Pants = GetAllAffixes("Pants");
            mobalyticsBuildVariant.Boots = GetAllAffixes("Boots");

            // Accessories
            mobalyticsBuildVariant.Amulet = GetAllAffixes("Amulet");
            mobalyticsBuildVariant.Ring.AddRange(GetAllAffixes("Ring 1"));
            mobalyticsBuildVariant.Ring.AddRange(GetAllAffixes("Ring 2"));
            mobalyticsBuildVariant.Ring = mobalyticsBuildVariant.Ring.Distinct().ToList();

            // Weapons
            mobalyticsBuildVariant.Weapon.AddRange(GetAllAffixes("Weapon"));
            mobalyticsBuildVariant.Weapon.AddRange(GetAllAffixes("Bludgeoning Weapon"));
            mobalyticsBuildVariant.Weapon.AddRange(GetAllAffixes("Slashing Weapon"));
            mobalyticsBuildVariant.Weapon.AddRange(GetAllAffixes("Dual-Wield Weapon 1"));
            mobalyticsBuildVariant.Weapon.AddRange(GetAllAffixes("Dual-Wield Weapon 2"));
            mobalyticsBuildVariant.Weapon = mobalyticsBuildVariant.Weapon.Distinct().ToList();
            mobalyticsBuildVariant.Ranged = GetAllAffixes("Ranged Weapon");
            mobalyticsBuildVariant.Offhand = GetAllAffixes("Offhand");

            // Look for runes container
            // "Active Runes"
            // Runes
            mobalyticsBuildVariant.Runes = GetAllRunes();

            // Process "Paragon" tab
            if (_settingsManager.Settings.IsImportParagonMobalyticsEnabled)
            {
                // Look for paragon container
                // "Paragon Board"
                header = "Paragon Board";
                var paragonBoardHeader = _webDriver.FindElement(By.XPath($"//header[./div[contains(text(), '{header}')]]")).FindElements(By.TagName("div"));

                // Paragon Board
                _ = _webDriver?.ExecuteScript("arguments[0].click();", paragonBoardHeader[2]);
                Thread.Sleep(_delayClickParagon);
                mobalyticsBuildVariant.ParagonBoards = GetAllParagonBoards(mobalyticsBuild);
            }

            mobalyticsBuild.Variants.Add(mobalyticsBuildVariant);
            _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Exported {variantName}." });

            // Reset Timeout
            _webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(10 * 1000);
        }

        private List<MobalyticsAffix> GetAllAffixes(string itemType)
        {
            try
            {
                List<MobalyticsAffix> affixes = new List<MobalyticsAffix>();

                string header = "Gear Stats";
                var affixContainer = _webDriver.FindElement(By.XPath($"//div[./header[./div[contains(text(), '{header}')]]]"))
                    .FindElement(By.XPath(".//div/div[1]"))
                    .FindElements(By.XPath("div"));

                // Find element that contains the current itemType
                var affixContainerType = affixContainer.FirstOrDefault(e =>
                {
                    var elements = e.FindElements(By.XPath($".//div/div/div/span[1]"));
                    if (elements.Any())
                    {
                        return elements[0].Text.Equals(itemType);
                    }

                    return false;
                });

                // Find unique / aspect info
                string aspectsOrUniqueDescription = string.Empty;
                if (affixContainerType != null)
                {
                    var elements = affixContainerType.FindElements(By.XPath($".//div/div/div/span[2]"));
                    aspectsOrUniqueDescription = elements.Any() ? elements[0].Text : string.Empty;
                }
                bool isUnique = !string.IsNullOrWhiteSpace(aspectsOrUniqueDescription) &&
                    !aspectsOrUniqueDescription.Equals("Empty", StringComparison.InvariantCultureIgnoreCase) &&
                    !aspectsOrUniqueDescription.Contains("Aspect", StringComparison.InvariantCultureIgnoreCase);

                if (isUnique && !_settingsManager.Settings.IsImportUniqueAffixesMobalyticsEnabled)
                {
                    return affixes;
                }

                if (affixContainerType != null)
                {
                    //var asHtml = affixContainerType.GetAttribute("innerHTML");

                    // Find the list items with affixes
                    var elementAffixes = affixContainerType.FindElements(By.TagName("li"));
                    foreach (var elementAffix in elementAffixes)
                    {
                        MobalyticsAffix mobalyticsAffix = new MobalyticsAffix();
                        var asHtml = elementAffix.GetAttribute("innerHTML");

                        var elementSpans = elementAffix.FindElements(By.TagName("span"));
                        string affix = elementSpans.Count == 1 || (elementSpans.Count > 1 && string.IsNullOrWhiteSpace(elementSpans[1].Text)) ? elementSpans[0].Text :
                            elementSpans[0].Text.Replace(elementSpans[1].Text, string.Empty).Trim();

                        mobalyticsAffix.IsGreater = asHtml.Contains("Greater.svg");
                        mobalyticsAffix.IsImplicit = asHtml.Contains(">Implicit</span>");
                        mobalyticsAffix.IsTempered = asHtml.Contains("Tempreing.svg") || asHtml.Contains("Tempering.svg");

                        if(mobalyticsAffix.IsImplicit || mobalyticsAffix.IsTempered)
                        {
                            affix = affix.Contains(":") ? affix.Substring(affix.IndexOf(":") + 1) : affix;
                            affix = affix.Trim();
                        }

                        mobalyticsAffix.AffixText = affix;
                        affixes.Add(mobalyticsAffix);
                    }
                }
                return affixes;
            }
            catch (Exception)
            {
                return new();
            }
        }

        private List<string> GetAllAspects()
        {
            try
            {
                string header = "Aspects & Uniques";
                var aspectContainer = _webDriver.FindElement(By.XPath($"//div[./header[./div[contains(text(), '{header}')]]]"))
                    .FindElement(By.XPath(".//div/div[1]"))
                    .FindElements(By.XPath("div"));

                List<string> aspects = new List<string>();
                foreach (var aspect in aspectContainer)
                {
                    var description = aspect.Text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in description)
                    {
                        if (line.Contains("Aspect", StringComparison.OrdinalIgnoreCase))
                        {
                            aspects.Add(line);
                            break;
                        }
                    }
                }
                return aspects;
            }
            catch (Exception)
            {
                return new();
            }
        }

        private List<string> GetAllRunes()
        {
            try
            {
                // Look for runes container
                // "Active Runes"
                string header = "Active Runes";
                List<string> runes = _webDriver.FindElement(By.XPath($"//div[./header[contains(text(), '{header}')]]"))
                    .FindElement(By.XPath(".//div/div[1]"))
                    .FindElements(By.TagName("span")).Select(e => e.Text).Where(t => !t.Equals("Empty") && !t.Equals("Rune")).ToList();

                return runes;
            }
            catch (Exception)
            {
                return new();
            }
        }

        private List<string> GetAllUniques()
        {
            try
            {
                string header = "Aspects & Uniques";
                var uniqueContainer = _webDriver.FindElement(By.XPath($"//div[./header[./div[contains(text(), '{header}')]]]"))
                    .FindElement(By.XPath(".//div/div[1]"))
                    .FindElements(By.XPath("div"));

                List<string> uniques = new List<string>();
                foreach (var unique in uniqueContainer)
                {
                    var description = unique.Text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    if (description.Length == 2 && !string.IsNullOrWhiteSpace(description[0]) && !string.IsNullOrWhiteSpace(description[1]))
                    {
                        if (!description[0].Contains("Aspect", StringComparison.OrdinalIgnoreCase))
                        {
                            uniques.Add(description[0]);
                        }
                    }
                }
                return uniques;
            }
            catch (Exception)
            {
                return new();
            }
        }

        private List<ParagonBoard> GetAllParagonBoards(MobalyticsBuild mobalyticsBuild)
        {
            List<ParagonBoard> paragonBoards = new List<ParagonBoard>();

            // Get all boards
            string header = "Paragon Board";
            System.Collections.ObjectModel.ReadOnlyCollection<IWebElement>? paragonContainer = null;

            int retryCount = 0;
            bool paragonBoardLoaded = false;
            while (retryCount < 3 && !paragonBoardLoaded)
            {
                // Allow multiple attemps to find paragon board. Loading can take multiple seconds.
                retryCount++;

                try
                {
                    paragonContainer = _webDriver.FindElement(By.XPath($"//div[./header[./div[contains(text(), '{header}')]]]"))
                    .FindElement(By.XPath(".//div/div[1]/div[1]/div[1]/div[2]/div[1]/div[2]/div[1]/div[1]/div[1]"))
                    .FindElements(By.XPath("div"));

                    paragonBoardLoaded = true;
                }
                catch (Exception ex)
                {
                    Thread.Sleep(_delayClickParagon);

                    _logger.LogError(ex, $"{MethodBase.GetCurrentMethod()?.Name} (Paragon board not found. Retry #{retryCount}/3)");
                    _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Loading paragon board. (Retry #{retryCount})" });
                }
            }

            var countBoards = paragonContainer?.Count ?? 0;
            for (int i = 0; i < countBoards; i++)
            {
                var nameAndGlyph = paragonContainer[i].FindElement(By.XPath(".//div/div[1]/div[2]")).GetAttribute("innerText").Split("/", StringSplitOptions.RemoveEmptyEntries);
                string name = nameAndGlyph.Length >= 1 ? nameAndGlyph[0].Trim() : string.Empty;
                string glyph = nameAndGlyph.Length >= 2 ? nameAndGlyph[1].Trim() : string.Empty;
                string rotateString = paragonContainer[i].GetAttribute("style");

                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Paragon: {name} ({glyph})." });

                // Convert rotate string
                int rotateInt = 0;
                string subStringBegin = "rotate(";
                string subStringEnd = "deg)";
                if (rotateString.Contains(subStringBegin))
                {
                    rotateString = rotateString.Substring(rotateString.IndexOf(subStringBegin) + subStringBegin.Length,
                        rotateString.IndexOf(subStringEnd) - (rotateString.IndexOf(subStringBegin) + subStringBegin.Length));
                    rotateInt = int.Parse(rotateString) % 360;
                }

                var paragonBoard = new ParagonBoard();
                paragonBoard.Name = name;
                paragonBoard.Glyph = glyph;
                string rotationInfo = rotateInt == 0 ? "0°" :
                                rotateInt == 90 ? "90°" :
                                rotateInt == 180 ? "180°" :
                                rotateInt == 270 ? "270°" : "?°";
                paragonBoard.Rotation = rotationInfo;
                paragonBoards.Add(paragonBoard);

                // Get all nodes
                var nodeElements = paragonContainer[i].FindElements(By.XPath($".//div[./span]"));
                var countNodes = nodeElements?.Count ?? 0;
                for (int j = 0; j < countNodes; j++)
                {
                    // left: 80em; top: 0em; transform: rotate(0deg); transition: transform 0.5s;
                    string positionString = nodeElements[j].GetAttribute("style");
                    string statusString = nodeElements[j].GetAttribute("innerHTML");
                    // opacity: 0.25 (Inactive)
                    // opacity: 1 (Active)
                    if (!positionString.Contains("left:") || !positionString.Contains("top:") || !statusString.Contains("opacity: 1")) continue;

                    var nodeInfo = positionString.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    int locationX = int.Parse(string.Concat(nodeInfo[0].Where(Char.IsDigit)));
                    int locationY = int.Parse(string.Concat(nodeInfo[1].Where(Char.IsDigit)));
                    locationX = (locationX / 8) + 1; // Conversion so same logic as D4Builds can be used.
                    locationY = (locationY / 8) + 1; // Conversion so same logic as D4Builds can be used.
                    int locationXT = locationX;
                    int locationYT = locationY;

                    if (rotateInt == 0 || positionString.Contains("rotate(0deg)"))
                    {
                        // Note: Also check positionString because gates are always set to 0 degrees.
                        locationXT = locationXT - 1;
                        locationYT = locationYT - 1;
                    }
                    else if (rotateInt == 90)
                    {
                        locationXT = 21 - locationY;
                        locationYT = locationX;
                        locationYT = locationYT - 1;
                    }
                    else if (rotateInt == 180)
                    {
                        locationXT = 21 - locationX;
                        locationYT = 21 - locationY;
                    }
                    else if (rotateInt == 270)
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

        private string GetBuildName()
        {
            try
            {
                var container = _webDriver.FindElement(By.Id("container"));
                string buildDescription = container.FindElements(By.TagName("h1"))[0].Text;
                return buildDescription.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)[1];
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private string GetLastUpdateInfo()
        {
            try
            {
                var container = _webDriver.FindElement(By.Id("container"));
                string lastUpdateInfo = container.FindElements(By.TagName("footer"))[0].Text;
                lastUpdateInfo = lastUpdateInfo.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)[1];
                return lastUpdateInfo;
            }
            catch (Exception)
            {
                return DateTime.Now.ToString();
            }
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

        #endregion
    }
}
