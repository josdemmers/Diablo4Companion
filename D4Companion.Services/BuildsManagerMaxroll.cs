using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace D4Companion.Services
{
    public class BuildsManagerMaxroll : IBuildsManagerMaxroll
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly IHttpClientHandler _httpClientHandler;
        private readonly ISettingsManager _settingsManager;

        private List<MaxrollBuild> _maxrollBuilds = new();
        private Dictionary<int, int> _maxrollMappingsAspects = new();

        // Start of Constructors region

        #region Constructors

        public BuildsManagerMaxroll(IEventAggregator eventAggregator, ILogger<BuildsManagerMaxroll> logger, IAffixManager affixManager, IHttpClientHandler httpClientHandler, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            // Init logger
            _logger = logger;

            // Init services
            _affixManager = affixManager;
            _httpClientHandler = httpClientHandler;
            _settingsManager = settingsManager;

            // Init sno mappings
            InitMaxrollMappingsAspects();

            // Load available Maxroll builds.
            Task.Factory.StartNew(() =>
            {
                LoadAvailableMaxrollBuilds();
            });
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<MaxrollBuild> MaxrollBuilds { get => _maxrollBuilds; set => _maxrollBuilds = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        private void InitMaxrollMappingsAspects()
        {
            _maxrollMappingsAspects.Clear();
            _maxrollMappingsAspects.Add(96035007, 1743946); // legendary_sorc_138 (of Shredding Blades).
        }

        public void CreatePresetFromMaxrollBuild(MaxrollBuild maxrollBuild, string profile, string name)
        {
            name = string.IsNullOrWhiteSpace(name) ? maxrollBuild.Name : name;

            // Note: Only allow one Maxroll build. Update if already exists.
            _affixManager.AffixPresets.RemoveAll(p => p.Name.Equals(name));

            var affixPreset = new AffixPreset
            {
                Name = name
            };

            var maxrollBuildDataProfileJson = maxrollBuild.Data.Profiles.FirstOrDefault(p => p.Name.Equals(profile));
            if (maxrollBuildDataProfileJson != null)
            {
                List<int> aspects = new List<int>();
                string itemType = string.Empty;

                // Loop through all items
                foreach (var item in maxrollBuildDataProfileJson.Items)
                {
                    switch(item.Key)
                    {
                        case 4: // Helm
                            itemType = Constants.ItemTypeConstants.Helm;
                            break;
                        case 5: // Chest
                            itemType = Constants.ItemTypeConstants.Chest;
                            break;
                        case 6: // 1HTotem
                            itemType = Constants.ItemTypeConstants.Offhand;
                            break;
                        case 7: // 1HAxe
                        case 8: // 2HMace
                        case 9: // 2HAxe
                        case 11: // 1HMace, 1HSword
                        case 12: // 1HMace, 1HSword
                            itemType = Constants.ItemTypeConstants.Weapon;
                            break;
                        case 10: // 2HCrossbow
                            itemType = Constants.ItemTypeConstants.Ranged;
                            break;
                        case 13: // Gloves
                            itemType = Constants.ItemTypeConstants.Gloves;
                            break;
                        case 14: // Pants
                            itemType = Constants.ItemTypeConstants.Pants;
                            break;
                        case 15: // Boots
                            itemType = Constants.ItemTypeConstants.Boots;
                            break;
                        case 16: // Ring
                        case 17: // Ring
                            itemType = Constants.ItemTypeConstants.Ring;
                            break;
                        case 18: // Amulet
                            itemType = Constants.ItemTypeConstants.Amulet;
                            break;
                        default:
                            _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown itemtype id: {item.Key}");
                            _eventAggregator.GetEvent<WarningOccurredEvent>().Publish(new WarningOccurredEventParams
                            {
                                Message = $"Imported Maxroll build contains unknown itemtype id: {item.Key}."
                            });
                            continue;
                    }

                    // Skip unique items
                    if (maxrollBuild.Data.Items[item.Value].Id.Contains("Unique", StringComparison.OrdinalIgnoreCase)) continue;

                    // Add all explicit affixes for current item.Value
                    foreach (var explicitAffix in maxrollBuild.Data.Items[item.Value].Explicits)
                    {
                        int affixSno = explicitAffix.Nid;
                        AffixInfo? affixInfoFull = _affixManager.GetAffixInfoFromFull(affixSno);

                        if (affixInfoFull == null)
                        {
                            _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown affix sno: {affixSno}");
                            _eventAggregator.GetEvent<WarningOccurredEvent>().Publish(new WarningOccurredEventParams
                            {
                                Message = $"Imported Maxroll build contains unknown affix sno: {affixSno}."
                            });
                        }
                        else
                        {
                            AffixInfo? affixInfo = _affixManager.GetAffixInfo(affixInfoFull);
                            if (affixInfo == null)
                            {
                                _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown affix: ({affixInfoFull.IdSno}) {affixInfoFull.IdName}");
                                _eventAggregator.GetEvent<WarningOccurredEvent>().Publish(new WarningOccurredEventParams
                                {
                                    Message = $"Imported Maxroll build contains unknown affix: ({affixInfoFull.IdSno}) {affixInfoFull.IdName}"
                                });
                            }
                            else
                            {
                                if (!affixPreset.ItemAffixes.Any(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType)))
                                {
                                    affixPreset.ItemAffixes.Add(new ItemAffix
                                    {
                                        Id = affixInfo.IdName,
                                        Type = itemType
                                    });
                                }
                            }
                        }
                    }

                    // Add all tempered affixes for current item.Value
                    foreach (var temperedAffix in maxrollBuild.Data.Items[item.Value].Tempered)
                    {
                        int affixSno = temperedAffix.Nid;
                        AffixInfo? affixInfoFull = _affixManager.GetAffixInfoFromFull(affixSno);

                        if (affixInfoFull == null)
                        {
                            _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown tempered affix sno: {affixSno}");
                            _eventAggregator.GetEvent<WarningOccurredEvent>().Publish(new WarningOccurredEventParams
                            {
                                Message = $"Imported Maxroll build contains unknown tempered affix sno: {affixSno}."
                            });
                        }
                        else
                        {
                            AffixInfo? affixInfo = _affixManager.GetAffixInfo(affixInfoFull);
                            if (affixInfo == null)
                            {
                                _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown tempered affix: ({affixInfoFull.IdSno}) {affixInfoFull.IdName}");
                                _eventAggregator.GetEvent<WarningOccurredEvent>().Publish(new WarningOccurredEventParams
                                {
                                    Message = $"Imported Maxroll build contains unknown tempered affix: ({affixInfoFull.IdSno}) {affixInfoFull.IdName}"
                                });
                            }
                            else
                            {
                                if (!affixPreset.ItemAffixes.Any(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType)))
                                {
                                    affixPreset.ItemAffixes.Add(new ItemAffix
                                    {
                                        Id = affixInfo.IdName,
                                        Type = itemType
                                    });
                                }
                            }
                        }
                    }

                    // Find all aspects / legendary powers
                    int legendaryPower = maxrollBuild.Data.Items[item.Value].LegendaryPower.Nid;
                    if (legendaryPower != 0)
                    {
                        aspects.Add(legendaryPower);
                    }
                }

                // Add all aspects to preset
                foreach (var aspectSnoFA in aspects) 
                {
                    int aspectSno = aspectSnoFA;
                    string aspectId = _affixManager.GetAspectId(aspectSno);

                    if (string.IsNullOrWhiteSpace(aspectId))
                    {
                        // Check if there is a known mapping available
                        if (_maxrollMappingsAspects.TryGetValue(aspectSno, out int aspectSnoMapped))
                        {
                            aspectSno = aspectSnoMapped;
                            aspectId = _affixManager.GetAspectId(aspectSno);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(aspectId))
                    {
                        _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown aspect sno: {aspectSno}");
                        _eventAggregator.GetEvent<WarningOccurredEvent>().Publish(new WarningOccurredEventParams
                        {
                            Message = $"Imported Maxroll build contains unknown aspect sno: {aspectSno}."
                        });
                    }
                    else
                    {
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Helm });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Chest });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Gloves });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Pants });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Boots });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Amulet });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Ring });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Weapon });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Ranged });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Offhand });
                    }
                }

                _affixManager.AddAffixPreset(affixPreset);
            }
        }

        public async void DownloadMaxrollBuild(string build)
        {
            try
            {
                string uri = $"https://planners.maxroll.gg/profiles/d4/{build}";

                string json = await _httpClientHandler.GetRequest(uri);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    MaxrollBuildJson? maxrollBuildJson = JsonSerializer.Deserialize<MaxrollBuildJson>(json);
                    if (maxrollBuildJson != null)
                    {
                        MaxrollBuildDataJson? maxrollBuildDataJson = null;
                        maxrollBuildDataJson = JsonSerializer.Deserialize<MaxrollBuildDataJson>(maxrollBuildJson.Data);
                        if (maxrollBuildJson != null)
                        {
                            // Valid json - Save and refresh available builds.
                            Directory.CreateDirectory(@".\Builds\Maxroll");
                            File.WriteAllText(@$".\Builds\Maxroll\{build}.json", json);
                            LoadAvailableMaxrollBuilds();
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"Invalid response. uri: {uri}");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        public void RemoveMaxrollBuild(string buildId)
        {
            string directory = @".\Builds\Maxroll";
            File.Delete(@$"{directory}\{buildId}.json");
            LoadAvailableMaxrollBuilds();
        }

        private void LoadAvailableMaxrollBuilds()
        {
            try
            {
                MaxrollBuilds.Clear();

                string directory = @".\Builds\Maxroll";
                if (Directory.Exists(directory))
                {
                    var fileEntries = Directory.EnumerateFiles(directory).Where(tooltip => tooltip.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                    foreach (string fileName in fileEntries)
                    {
                        string json = File.ReadAllText(fileName);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            MaxrollBuildJson? maxrollBuildJson = JsonSerializer.Deserialize<MaxrollBuildJson>(json);
                            if (maxrollBuildJson != null)
                            {
                                MaxrollBuildDataJson? maxrollBuildDataJson = null;
                                maxrollBuildDataJson = JsonSerializer.Deserialize<MaxrollBuildDataJson>(maxrollBuildJson.Data);
                                if (maxrollBuildDataJson != null)
                                {
                                    MaxrollBuild maxrollBuild = new MaxrollBuild
                                    {
                                        Data = maxrollBuildDataJson,
                                        Date = maxrollBuildJson.Date,
                                        Id = maxrollBuildJson.Id,
                                        Name = maxrollBuildJson.Name
                                    };

                                    MaxrollBuilds.Add(maxrollBuild);
                                }
                            }
                        }
                    }

                    _eventAggregator.GetEvent<MaxrollBuildsLoadedEvent>().Publish();
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
