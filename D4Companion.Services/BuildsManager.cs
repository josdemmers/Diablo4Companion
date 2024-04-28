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
    public class BuildsManager : IBuildsManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly IHttpClientHandler _httpClientHandler;
        private readonly ISettingsManager _settingsManager;

        private List<MaxrollBuild> _maxrollBuilds = new();
        private Dictionary<int, int> _maxrollMappings = new();
        private Dictionary<int, int> _maxrollMappingsAspects = new();

        // Start of Constructors region

        #region Constructors

        public BuildsManager(IEventAggregator eventAggregator, ILogger<BuildsManager> logger, IAffixManager affixManager, IHttpClientHandler httpClientHandler, ISettingsManager settingsManager)
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
            InitMaxrollMappings();
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

        private void InitMaxrollMappings()
        {

            //_maxrollMappings.Add(1669403, 000); // PassiveRankBonus_Rogue_Discipline_T3_N4_Scaled2H --> xxx (Ranks of the Concussive Passive)

            //PassiveRankBonus_Rogue_Discipline_T3_N2_Impetus

            _maxrollMappings.Clear();
            _maxrollMappings.Add(1193773, 577217); // LuckJewelry --> Luck
            _maxrollMappings.Add(1193845, 577051); // CritChanceJewelry --> CritChance
            _maxrollMappings.Add(1227911, 577173); // Life_Flat_Quadruple_UBERUNIQUE --> Life
            _maxrollMappings.Add(1227940, 583482); // CoreStats_All_Double_UBERUNIQUE --> CoreStats_All
            _maxrollMappings.Add(1235087, 577093); // Damage_Double_UBERUNIQUE --> Damage
            _maxrollMappings.Add(1316341, 577035); // Resource_Cost_Reduction_Barbarian_Lesser --> Resource_Cost_Reduction_Barbarian
            _maxrollMappings.Add(1316343, 577037); // Resource_Cost_Reduction_Druid_Lesser --> Resource_Cost_Reduction_Druid
            _maxrollMappings.Add(1316345, 577033); // Resource_Cost_Reduction_Necromancer_Lesser --> Resource_Cost_Reduction_Necromancer
            _maxrollMappings.Add(1316347, 577034); // Resource_Cost_Reduction_Rogue_Lesser --> Resource_Cost_Reduction_Rogue
            _maxrollMappings.Add(1316349, 577036); // Resource_Cost_Reduction_Sorcerer_Lesser --> Resource_Cost_Reduction_Sorcerer
            _maxrollMappings.Add(1316491, 1290914); // Luck_With_Barrier_Jewelry --> Luck_With_Barrier
            _maxrollMappings.Add(1320722, 577093); // Damage_FullScaling --> Damage
            _maxrollMappings.Add(1320724, 577173); // LifePercent_Double_UBERUNIQUE --> Life
            _maxrollMappings.Add(1321863, 583482); // CoreStats_All_Weapon --> CoreStats_All
            _maxrollMappings.Add(1321865, 583654); // CoreStat_Willpower_Weapon --> CoreStat_Willpower
            _maxrollMappings.Add(1321867, 583632); // CoreStat_Strength_Weapon --> CoreStat_Strength
            _maxrollMappings.Add(1321869, 583646); // CoreStat_Intelligence_Weapon --> CoreStat_Intelligence
            _maxrollMappings.Add(1321871, 583643); // CoreStat_Dexterity_Weapon --> CoreStat_Dexterity
            _maxrollMappings.Add(1322044, 577053); // OverpowerDamage_Jewelry --> OverpowerDamage
            _maxrollMappings.Add(1322163, 1091321); // PassiveRankBonus_Sorc_Elemental_T2_N2_Always1 --> PassiveRankBonus_Sorc_Elemental_T2_N1_GlassCannon
            _maxrollMappings.Add(1322165, 1091984); // PassiveRankBonus_Rogue_Discipline_T3_N3_Scaled2H --> PassiveRankBonus_Rogue_Discipline_T3_N2_Impetus
            _maxrollMappings.Add(1322167, 1091982); // PassiveRankBonus_Rogue_Cunning_T3_N2_Scaled2H --> PassiveRankBonus_Rogue_Cunning_T3_N1_Exploit
            _maxrollMappings.Add(1341729, 577173); // Life_Greater --> Life
            _maxrollMappings.Add(1439263, 1290758); // Damage_Type_Bonus_NonPhysical_Greater --> Damage_Type_Bonus_NonPhysical
            _maxrollMappings.Add(1439265, 577213); // Movement_Speed_Lesser --> Movement_Speed
            _maxrollMappings.Add(1480001, 1084591); // Resource_MaxEssence_Jewelry --> Resource_MaxEssence
            _maxrollMappings.Add(1480004, 577017); // Resource_MaxFury_Jewelry --> Resource_MaxFury
            _maxrollMappings.Add(1639491, 1085169); // Damage_Category_Spenders_UBERUNIQUE --> Damage_Category_Spenders
            _maxrollMappings.Add(1639493, 577093); // Damage_FullScaling_UBERUNIQUE --> Damage
            _maxrollMappings.Add(1639495, 577203); // Lucky_Hit_Heal_Life_UBERUNIQUE --> Lucky_Hit_Heal_Life
            _maxrollMappings.Add(1639541, 1341724); // ResourceGain_UBERUNIQUE --> ResourceGain
            _maxrollMappings.Add(1639572, 577021); // CD_Reduction_UBERUNIQUE --> CD_Reduction
            _maxrollMappings.Add(1639574, 583482); // CoreStats_All_Weapon_UBERUNIQUE --> CoreStats_All
            _maxrollMappings.Add(1639807, 577217); // LuckJewelry_UBERUNIQUE --> Luck
            _maxrollMappings.Add(1639811, 577051); // CritChanceJewelry_UBERUNIQUE --> CritChance
            _maxrollMappings.Add(1639815, 577177); // CritDamage_UBERUNIQUE --> CritDamage
            _maxrollMappings.Add(1664583, 1087398); // Damage_Category_Ultimate_LessThanTriple_UNIQUE --> Damage_Category_Ultimate
            _maxrollMappings.Add(1669468, 577093); // Damage_Greater_UNIQUE --> Damage
            _maxrollMappings.Add(1669540, 1088076); // SkillRankBonus_Generic_Category_Core_Always1_UNIQUE --> SkillRankBonus_Generic_Category_Core
            _maxrollMappings.Add(1730620, 577017); // Resource_MaxFury_Greater_Jewelry_Unique --> Resource_MaxFury
            _maxrollMappings.Add(1730631, 1341724); // ResourceGain_Greater_UNIQUE --> ResourceGain
        }

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
                        string affixId = _affixManager.GetAffixId(affixSno);

                        if (string.IsNullOrWhiteSpace(affixId))
                        {
                            // Check if there is a known mapping available
                            if (_maxrollMappings.TryGetValue(affixSno, out int affixSnoMapped))
                            {
                                affixSno = affixSnoMapped;
                                affixId = _affixManager.GetAffixId(affixSno);
                            }
                        }

                        bool isUniqueAffix = false;
                        if (string.IsNullOrWhiteSpace(affixId))
                        {
                            // Ignore unique item affix
                            isUniqueAffix = _affixManager.IsUniqueAffix(affixSno);
                        }

                        if (string.IsNullOrWhiteSpace(affixId) && !isUniqueAffix)
                        {
                            _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown affix sno: {affixSno}");
                            _eventAggregator.GetEvent<WarningOccurredEvent>().Publish(new WarningOccurredEventParams
                            {
                                Message = $"Imported Maxroll build contains unknown affix sno: {affixSno}."
                            });
                        }
                        else
                        {
                            if (!affixPreset.ItemAffixes.Any(a => a.Id.Equals(affixId) && a.Type.Equals(itemType)) && !isUniqueAffix)
                            {
                                affixPreset.ItemAffixes.Add(new ItemAffix
                                {
                                    Id = affixId,
                                    Type = itemType
                                });
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
