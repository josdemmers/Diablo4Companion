using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using FuzzierSharp;
using FuzzierSharp.SimilarityRatio;
using FuzzierSharp.SimilarityRatio.Scorer.Composite;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Prism.Events;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml.Linq;

namespace D4Companion.Services
{
    public class BuildsManagerD4Builds : IBuildsManagerD4Builds
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly ISettingsManager _settingsManager;

        private static readonly int _delayVariant = 100;

        private List<AffixInfo> _affixes = new List<AffixInfo>();
        private List<string> _affixDescriptions = new List<string>();
        private Dictionary<string, string> _affixMapDescriptionToId = new Dictionary<string, string>();
        private List<AspectInfo> _aspects = new List<AspectInfo>();
        private List<string> _aspectNames = new List<string>();
        private Dictionary<string, string> _aspectMapNameToId = new Dictionary<string, string>();
        private List<D4BuildsBuild> _d4BuildsBuilds = new();
        private WebDriver? _webDriver = null;
        private WebDriverWait? _webDriverWait = null;

        // Start of Constructors region

        #region Constructors

        public BuildsManagerD4Builds(IEventAggregator eventAggregator, ILogger<BuildsManager> logger, IAffixManager affixManager, ISettingsManager settingsManager)
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

            // Init Selenium
            InitSelenium();

            // Load available D4Builds builds.
            Task.Factory.StartNew(() =>
            {
                LoadAvailableD4BuildsBuilds();
            });
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<D4BuildsBuild> D4BuildsBuilds { get => _d4BuildsBuilds; set => _d4BuildsBuilds = value; }

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
            _affixDescriptions = _affixes.Select(affix => affix.DescriptionClean).ToList();

            // Create dictionary to map affix description with affix id
            _affixMapDescriptionToId.Clear();
            _affixMapDescriptionToId = _affixes.ToDictionary(affix => affix.DescriptionClean, affix => affix.IdName);
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

        private void InitSelenium()
        {
            // Options: Headless, size, security, ...
            var options = new ChromeOptions();

            options.AddArgument("--headless");
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
        }

        public void CreatePresetFromD4BuildsBuild(D4BuildsBuildVariant d4BuildsBuild, string buildNameOriginal, string buildName)
        {
            buildName = string.IsNullOrWhiteSpace(buildName) ? buildNameOriginal : buildName;

            // Note: Only allow one D4Builds build. Update if already exists.
            _affixManager.AffixPresets.RemoveAll(p => p.Name.Equals(buildName));

            var affixPreset = d4BuildsBuild.AffixPreset;
            affixPreset.Name = buildName;
            _affixManager.AddAffixPreset(affixPreset);
        }

        public void DownloadD4BuildsBuild(string buildIdD4Builds)
        {
            try
            {
                if (_webDriver == null) return;
                if (_webDriverWait == null) return;

                if (_webDriver.SessionId == null) InitSelenium();

                D4BuildsBuild d4BuildsBuild = new D4BuildsBuild
                {
                    Id = buildIdD4Builds
                };

                //var watch = System.Diagnostics.Stopwatch.StartNew();
                _eventAggregator.GetEvent<D4BuildsStatusUpdateEvent>().Publish(new D4BuildsStatusUpdateEventParams { Build = d4BuildsBuild, Status = $"Downloading {d4BuildsBuild.Id}." });
                _webDriver.Navigate().GoToUrl($"https://d4builds.gg/builds/{buildIdD4Builds}/?var=0");
                _webDriverWait.Until(e => !string.IsNullOrEmpty(e.FindElement(By.Id("renameBuild")).GetAttribute("value")));
                //watch.Stop();
                //System.Diagnostics.Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name} (Navigate): Elapsed time: {watch.ElapsedMilliseconds}");

                // Build name
                d4BuildsBuild.Name = _webDriver.FindElement(By.Id("renameBuild")).GetAttribute("value");
                _eventAggregator.GetEvent<D4BuildsStatusUpdateEvent>().Publish(new D4BuildsStatusUpdateEventParams { Build = d4BuildsBuild, Status = $"Downloaded {d4BuildsBuild.Name}." });

                // Last update
                d4BuildsBuild.Date = _webDriver.FindElement(By.ClassName("builder__last__updated")).Text;

                // Variants
                //watch = System.Diagnostics.Stopwatch.StartNew();
                ExportBuildVariants(d4BuildsBuild);
                //watch.Stop();
                //System.Diagnostics.Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name} (Export): Elapsed time: {watch.ElapsedMilliseconds}");
                //watch = System.Diagnostics.Stopwatch.StartNew();
                ConvertBuildVariants(d4BuildsBuild);
                //watch.Stop();
                //System.Diagnostics.Debug.WriteLine($"{MethodBase.GetCurrentMethod()?.Name} (Convert): Elapsed time: {watch.ElapsedMilliseconds}");

                // Save
                Directory.CreateDirectory(@".\Builds\D4Builds");
                using (FileStream stream = File.Create(@$".\Builds\D4Builds\{d4BuildsBuild.Id}.json"))
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    JsonSerializer.Serialize(stream, d4BuildsBuild, options);
                }
                LoadAvailableD4BuildsBuilds();

                _eventAggregator.GetEvent<D4BuildsStatusUpdateEvent>().Publish(new D4BuildsStatusUpdateEventParams { Build = d4BuildsBuild, Status = $"Done." });
                _eventAggregator.GetEvent<D4BuildsCompletedEvent>().Publish();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
            }
            finally
            {
                _webDriver?.Quit();
            }
        }

        private void ConvertBuildVariants(D4BuildsBuild d4BuildsBuild)
        {
            foreach (var variant in d4BuildsBuild.Variants)
            {
                _eventAggregator.GetEvent<D4BuildsStatusUpdateEvent>().Publish(new D4BuildsStatusUpdateEventParams { Build = d4BuildsBuild, Status = $"Converting {variant.Name}." });

                var affixPreset = new AffixPreset
                {
                    Name = variant.Name
                };

                // Prepare affixes
                List<Tuple<string,string>> affixesD4Builds = new List<Tuple<string,string>>();

                foreach (var affixD4Builds in variant.Helm)
                {
                    affixesD4Builds.Add(new Tuple<string, string>(Constants.ItemTypeConstants.Helm, affixD4Builds));
                }
                foreach (var affixD4Builds in variant.Chest)
                {
                    affixesD4Builds.Add(new Tuple<string, string>(Constants.ItemTypeConstants.Chest, affixD4Builds));
                }
                foreach (var affixD4Builds in variant.Gloves)
                {
                    affixesD4Builds.Add(new Tuple<string, string>(Constants.ItemTypeConstants.Gloves, affixD4Builds));
                }
                foreach (var affixD4Builds in variant.Pants)
                {
                    affixesD4Builds.Add(new Tuple<string, string>(Constants.ItemTypeConstants.Pants, affixD4Builds));
                }
                foreach (var affixD4Builds in variant.Boots)
                {
                    affixesD4Builds.Add(new Tuple<string, string>(Constants.ItemTypeConstants.Boots, affixD4Builds));
                }
                foreach (var affixD4Builds in variant.Amulet)
                {
                    affixesD4Builds.Add(new Tuple<string, string>(Constants.ItemTypeConstants.Amulet, affixD4Builds));
                }
                foreach (var affixD4Builds in variant.Ring)
                {
                    affixesD4Builds.Add(new Tuple<string, string>(Constants.ItemTypeConstants.Ring, affixD4Builds));
                }
                foreach (var affixD4Builds in variant.Weapon)
                {
                    affixesD4Builds.Add(new Tuple<string, string>(Constants.ItemTypeConstants.Weapon, affixD4Builds));
                }
                foreach (var affixD4Builds in variant.Ranged)
                {
                    affixesD4Builds.Add(new Tuple<string, string>(Constants.ItemTypeConstants.Ranged, affixD4Builds));
                }
                foreach (var affixD4Builds in variant.Offhand)
                {
                    affixesD4Builds.Add(new Tuple<string, string>(Constants.ItemTypeConstants.Offhand, affixD4Builds));
                }

                // Find matching affix ids
                ConcurrentBag<ItemAffix> itemAffixBag = new ConcurrentBag<ItemAffix>();
                Parallel.ForEach(affixesD4Builds, affixD4Builds =>
                {
                    var itemAffixResult = ConvertItemAffix(affixD4Builds);
                    itemAffixBag.Add(itemAffixResult);
                });
                affixPreset.ItemAffixes.AddRange(itemAffixBag);

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
                    affixPreset.ItemAspects.Add(new ItemAffix { Id = aspect.Id, Type = Constants.ItemTypeConstants.Aspect });
                }

                variant.AffixPreset = affixPreset;
                _eventAggregator.GetEvent<D4BuildsStatusUpdateEvent>().Publish(new D4BuildsStatusUpdateEventParams { Build = d4BuildsBuild, Status = $"Converted {variant.Name}." });
            }
        }

        private ItemAffix ConvertItemAffix(Tuple<string,string> affixD4Builds)
        {
            string affixId = string.Empty;
            string itemType = affixD4Builds.Item1;

            var result = Process.ExtractOne(affixD4Builds.Item2, _affixDescriptions, scorer: ScorerCache.Get<WeightedRatioScorer>());
            affixId = _affixMapDescriptionToId[result.Value];

            return new ItemAffix
            {
                Id = affixId,
                Type = itemType
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
                Type = Constants.ItemTypeConstants.Helm
            };
        }

        private void ExportBuildVariants(D4BuildsBuild d4BuildsBuild)
        {
            var count = _webDriver?.FindElements(By.ClassName("variant__button")).Count;
            for (int i = 0; i < count; i++)
            {
                _ = _webDriver?.ExecuteScript($"document.querySelectorAll('.variant__button')[{i}].click()");
                Thread.Sleep(_delayVariant);
                ExportBuildVariant(i, d4BuildsBuild);
            }
        }

        private void ExportBuildVariant(int variantIndex, D4BuildsBuild d4BuildsBuild)
        {
            // Set timeout to improve performance
            // https://stackoverflow.com/questions/16075997/iselementpresent-is-very-slow-in-case-if-element-does-not-exist
            _webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(0);

            string variantName = _webDriver.FindElement(By.Id($"renameVariant{variantIndex}")).GetAttribute("value");
            _eventAggregator.GetEvent<D4BuildsStatusUpdateEvent>().Publish(new D4BuildsStatusUpdateEventParams { Build = d4BuildsBuild, Status = $"Exporting {variantName}." });

            var d4BuildsBuildVariant = new D4BuildsBuildVariant
            {
                Name = variantName
            };

            // Aspects
            d4BuildsBuildVariant.Aspect = GetAllAspects();

            // Armor
            d4BuildsBuildVariant.Helm = GetAllAffixes("Helm");
            d4BuildsBuildVariant.Chest = GetAllAffixes("ChestArmor");
            d4BuildsBuildVariant.Gloves = GetAllAffixes("Gloves");
            d4BuildsBuildVariant.Pants = GetAllAffixes("Pants");
            d4BuildsBuildVariant.Boots = GetAllAffixes("Boots");

            // Accessories
            d4BuildsBuildVariant.Amulet = GetAllAffixes("Amulet");
            d4BuildsBuildVariant.Ring.AddRange(GetAllAffixes("Ring1"));
            d4BuildsBuildVariant.Ring.AddRange(GetAllAffixes("Ring2"));
            d4BuildsBuildVariant.Ring = d4BuildsBuildVariant.Ring.Distinct().ToList();

            // Weapons
            d4BuildsBuildVariant.Weapon.AddRange(GetAllAffixes("Weapon"));
            d4BuildsBuildVariant.Weapon.AddRange(GetAllAffixes("BludgeoningWeapon"));
            d4BuildsBuildVariant.Weapon.AddRange(GetAllAffixes("SlashingWeapon"));
            d4BuildsBuildVariant.Weapon.AddRange(GetAllAffixes("WieldWeapon1"));
            d4BuildsBuildVariant.Weapon.AddRange(GetAllAffixes("WieldWeapon2"));
            d4BuildsBuildVariant.Weapon = d4BuildsBuildVariant.Weapon.Distinct().ToList();
            d4BuildsBuildVariant.Ranged = GetAllAffixes("RangedWeapon");
            d4BuildsBuildVariant.Offhand = GetAllAffixes("Offhand");

            d4BuildsBuild.Variants.Add(d4BuildsBuildVariant);
            _eventAggregator.GetEvent<D4BuildsStatusUpdateEvent>().Publish(new D4BuildsStatusUpdateEventParams { Build = d4BuildsBuild, Status = $"Exported {variantName}." });

            // Reset Timeout
            _webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(10 * 1000);
        }

        private List<string> GetAllAspects()
        {
            try
            {
                return _webDriver.FindElements(By.ClassName("builder__gear__name")).Select(e => e.Text).Where(e => e.Contains("Aspect")).ToList();
            }
            catch (Exception)
            {
                return new();
            }
        }

        private List<string> GetAllAffixes(string itemType)
        {
            try
            {
                return _webDriver.FindElement(By.ClassName(itemType)).FindElements(By.ClassName("filled")).Select(e => e.GetAttribute("innerText")).ToList();
            }
            catch (Exception)
            {
                return new();
            }
        }

        public void RemoveD4BuildsBuild(string buildId)
        {
            string directory = @".\Builds\D4Builds";
            File.Delete(@$"{directory}\{buildId}.json");
            LoadAvailableD4BuildsBuilds();
        }

        private void LoadAvailableD4BuildsBuilds()
        {
            try
            {
                D4BuildsBuilds.Clear();

                string directory = @".\Builds\D4Builds";
                if (Directory.Exists(directory))
                {
                    var fileEntries = Directory.EnumerateFiles(directory).Where(tooltip => tooltip.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                    foreach (string fileName in fileEntries)
                    {
                        string json = File.ReadAllText(fileName);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            D4BuildsBuild? d4BuildsBuild = JsonSerializer.Deserialize<D4BuildsBuild>(json);
                            if (d4BuildsBuild != null)
                            {
                                D4BuildsBuilds.Add(d4BuildsBuild);
                            }
                        }
                    }

                    _eventAggregator.GetEvent<D4BuildsBuildsLoadedEvent>().Publish();
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion
    }
}
