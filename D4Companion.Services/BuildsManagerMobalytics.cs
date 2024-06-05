using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Xps;

namespace D4Companion.Services
{
    public class BuildsManagerMobalytics : IBuildsManagerMobalytics
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
        private List<MobalyticsBuild> _mobalyticsBuilds = new();
        private WebDriver? _webDriver = null;
        private WebDriverWait? _webDriverWait = null;

        // Start of Constructors region

        #region Constructors

        public BuildsManagerMobalytics(IEventAggregator eventAggregator, ILogger<BuildsManagerD4Builds> logger, IAffixManager affixManager, ISettingsManager settingsManager)
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

            // Load available D4Builds builds.
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

        private void InitSelenium()
        {
            // Options: Headless, size, security, ...
            var options = new ChromeOptions();

            //options.AddArgument("--headless");
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
            //new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
            _webDriver = new ChromeDriver(service: service, options: options);
            _webDriverWait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10));
        }

        public void CreatePresetFromMobalyticsBuild(MobalyticsBuildVariant mobalyticsBuild, string buildNameOriginal, string buildName)
        {
            throw new NotImplementedException();
        }

        public void DownloadMobalyticsBuild(string buildUrl)
        {
            buildUrl = "https://mobalytics.gg/diablo-4/builds/necromancer/mages-necro-guide";
            string id = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(buildUrl));

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

                // TODO: Need a better _webDriverWait.Until or other check for page load status.
                //_webDriverWait.Until(e => !string.IsNullOrEmpty(e.FindElement(By.Id("renameBuild")).GetAttribute("value")));
                // Extra sleep to make sure page is loaded.
                Thread.Sleep(5000);

                // Build name
                var container = _webDriver.FindElement(By.Id("container"));
                string buildDescription = container.FindElements(By.TagName("h1"))[0].Text;
                mobalyticsBuild.Name = buildDescription.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)[1];
                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Downloaded {mobalyticsBuild.Name}." });

                // Last update
                mobalyticsBuild.Date = GetLastUpdateInfo();

                // Variants
                ExportBuildVariants(mobalyticsBuild);


                _eventAggregator.GetEvent<MobalyticsStatusUpdateEvent>().Publish(new MobalyticsStatusUpdateEventParams { Build = mobalyticsBuild, Status = $"Done." });


                /*
                
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

                
                */
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
                _webDriver?.Quit();
                _webDriver = null;
                _webDriverWait = null;

                _eventAggregator.GetEvent<MobalyticsCompletedEvent>().Publish();
            }
        }

        private void ExportBuildVariants(MobalyticsBuild mobalyticsBuild)
        {
            //var buttonElements = _webDriver?.FindElements(By.ClassName("variant__button"));
            //var count = buttonElements?.Count ?? 0;

            //for (int i = 0; i < count; i++)
            //{
            //    string variant = Regex.Match(buttonElements[i].GetAttribute("outerHTML"), @"(?:renameVariant)\d+").Value;
            //    int variantIndex = int.Parse(Regex.Match(variant, @"\d+").Value);

            //    _ = _webDriver?.ExecuteScript($"document.querySelectorAll('.variant__button')[{i}].click()");
            //    Thread.Sleep(_delayVariant);
            //    ExportBuildVariant(variantIndex, mobalyticsBuild);
            //}
        }

        private void ExportBuildVariant(int variantIndex, MobalyticsBuild mobalyticsBuild)
        {
            // Set timeout to improve performance
            // https://stackoverflow.com/questions/16075997/iselementpresent-is-very-slow-in-case-if-element-does-not-exist
            _webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(0);


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

                    // TODO: mobalyticsBuild loaded event
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
            throw new NotImplementedException();
        }

        #endregion
    }
}
