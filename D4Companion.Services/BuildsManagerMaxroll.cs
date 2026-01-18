using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Interfaces;
using D4Companion.Messages;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace D4Companion.Services
{
    public class BuildsManagerMaxroll : IBuildsManagerMaxroll
    {
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly IHttpClientHandler _httpClientHandler;
        private readonly ISettingsManager _settingsManager;

        private List<MaxrollBuild> _maxrollBuilds = new();
        private Dictionary<int, int> _maxrollMappingsAspects = new();

        // Start of Constructors region

        #region Constructors

        public BuildsManagerMaxroll(ILogger<BuildsManagerMaxroll> logger, IAffixManager affixManager, IHttpClientHandler httpClientHandler, ISettingsManager settingsManager)
        {
            // Init services
            _affixManager = affixManager;
            _httpClientHandler = httpClientHandler;
            _logger = logger;
            _settingsManager = settingsManager;

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
                    switch (item.Key)
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
                            WeakReferenceMessenger.Default.Send(new WarningOccurredMessage(new WarningOccurredMessageParams
                            {
                                Message = $"Imported Maxroll build contains unknown itemtype id: {item.Key}."
                            }));
                            continue;
                    }

                    // Process runes
                    foreach (var socket in maxrollBuild.Data.Items[item.Value].Sockets)
                    {
                        string runeId = socket;
                        if (!runeId.StartsWith("Rune_", StringComparison.OrdinalIgnoreCase)) continue;

                        if (!affixPreset.ItemRunes.Any(r => r.Id.Equals($"Item_{runeId}")))
                        {
                            affixPreset.ItemRunes.Add(new ItemAffix { Id = $"Item_{runeId}", Type = Constants.ItemTypeConstants.Rune });
                        }
                    }

                    // Process unique items
                    string uniqueId = maxrollBuild.Data.Items[item.Value].Id;
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
                        if (!_settingsManager.Settings.IsImportUniqueAffixesMaxrollEnabled) continue;
                    }

                    // Process implicit affixes
                    if (uniqueInfo != null)
                    {
                        // Process unique implicit affixes.
                        if (itemType.Equals(Constants.ItemTypeConstants.Amulet) || 
                            itemType.Equals(Constants.ItemTypeConstants.Pants) ||
                            itemType.Equals(Constants.ItemTypeConstants.Ring))
                        {
                            // This item type no longer has implicit affixes.
                        }
                        else
                        {
                            foreach (var implicitAffix in maxrollBuild.Data.Items[item.Value].Implicits)
                            {
                                int affixSno = implicitAffix.Nid;

                                AffixInfo? affixInfo = _affixManager.GetAffixInfoMaxrollByIdSno(affixSno.ToString());
                                if (affixInfo == null)
                                {
                                    _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown implicit affix sno: {affixSno}");
                                    WeakReferenceMessenger.Default.Send(new WarningOccurredMessage(new WarningOccurredMessageParams
                                    {
                                        Message = $"Imported Maxroll build contains unknown implicit affix sno: {affixSno}."
                                    }));
                                }
                                else
                                {
                                    if (!affixPreset.ItemAffixes.Any(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType)))
                                    {
                                        affixPreset.ItemAffixes.Add(new ItemAffix
                                        {
                                            Id = affixInfo.IdName,
                                            Type = itemType,
                                            Color = _settingsManager.Settings.DefaultColorImplicit,
                                            IsImplicit = true
                                        });
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Process legendary implicit affixes.

                        // Note: Maxroll implicits are overruled by the website for each legendary type.
                        // The implicits set in the json data are ignored.

                        string itemId = maxrollBuild.Data.Items[item.Value].Id;
                        string itemTypeFromJson = GetItemTypeFromItemId(itemId);
                        List<string> affixNames = new List<string>();

                        switch (itemTypeFromJson)
                        {
                            case "1HAxe":
                            case "2HAxe":
                            case "2HStaff":
                                affixNames.Add("INHERENT_Damage_Over_Time");
                                break;
                            case "1HDagger":
                                affixNames.Add("INHERENT_Damage_to_Near");
                                break;
                            case "1HFocus":
                            case "1HTotem":
                                affixNames.Add("INHERENT_Luck");
                                break;
                            case "1HMace":
                            case "2HMace":
                                affixNames.Add("INHERENT_OverpowerDamage");
                                break;
                            case "1HScythe":
                            case "2HScythe":
                                affixNames.Add("INHERENT_Damage_Tag_Summoning");
                                break;
                            case "1HShield":
                                affixNames.Add("INHERENT_Block");
                                affixNames.Add("INHERENT_Shield_Damage_Bonus");
                                affixNames.Add("INHERENT_Thorns");
                                break;
                            case "1HSword":
                            case "2HSword":
                            case "2HBow":
                                affixNames.Add("INHERENT_CritDamage");
                                break;
                            case "1HWand":
                            case "2HCrossbow":
                            case "2HPolearm":
                                affixNames.Add("INHERENT_Damage_to_Vulnerable");
                                break;
                            case "2HQuarterstaff":
                                affixNames.Add("Block_Quarterstaff");
                                break;
                            case "2HGlaive":
                                affixNames.Add("INHERENT_Damage_to_Elite");
                                break;
                            case "Amulet":
                                break;
                            case "Boots":
                                // Note: Do not use a hardcoded affix for boots. Boots have different implicit affixes.
                                //affixNames.Add("INHERENT_Evade_Attack_Reset");
                                //affixNames.Add("INHERENT_Evade_Charges");
                                //affixNames.Add("INHERENT_Evade_MovementSpeed");
                                foreach (var implicitAffix in maxrollBuild.Data.Items[item.Value].Implicits)
                                {
                                    int affixSno = implicitAffix.Nid;

                                    AffixInfo? affixInfo = _affixManager.GetAffixInfoMaxrollByIdSno(affixSno.ToString());
                                    if (affixInfo != null)
                                    {
                                        if (!affixPreset.ItemAffixes.Any(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType)))
                                        {
                                            affixPreset.ItemAffixes.Add(new ItemAffix
                                            {
                                                Id = affixInfo.IdName,
                                                Type = itemType,
                                                Color = _settingsManager.Settings.DefaultColorImplicit,
                                                IsImplicit = true
                                            });
                                        }
                                    }
                                }
                                break;
                            case "Chest":
                                break;
                            case "Gloves":
                                break;
                            case "Helm":
                                break;
                            case "Pants":
                                break;
                            case "Ring":
                                break;
                            default:
                                _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Imported Maxroll build contains item type with unknown implicit affix: ({itemId}) {itemTypeFromJson}.");
                                WeakReferenceMessenger.Default.Send(new WarningOccurredMessage(new WarningOccurredMessageParams
                                {
                                    Message = $"Imported Maxroll build contains item type with no known implicit affix: ({itemId}) {itemTypeFromJson}."
                                }));
                                break;
                        }

                        foreach (var affix in affixNames)
                        {
                            AffixInfo? affixInfo = _affixManager.GetAffixInfoByIdName(affix);
                            if (affixInfo == null)
                            {
                                _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Imported Maxroll build contains unknown implicit affix name: {affix}.");
                                WeakReferenceMessenger.Default.Send(new WarningOccurredMessage(new WarningOccurredMessageParams
                                {
                                    Message = $"Imported Maxroll build contains unknown implicit affix name: {affix}."
                                }));
                                continue;
                            }

                            if (!affixPreset.ItemAffixes.Any(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType)))
                            {
                                affixPreset.ItemAffixes.Add(new ItemAffix
                                {
                                    Id = affixInfo.IdName,
                                    Type = itemType,
                                    Color = _settingsManager.Settings.DefaultColorImplicit,
                                    IsImplicit = true
                                });
                            }
                        }
                    }

                    // Add all explicit affixes for current item.Value
                    for (int i = 0; i < maxrollBuild.Data.Items[item.Value].Explicits.Count; i++)
                    {
                        // For legendary items only add the first four affixes.
                        if (uniqueInfo == null && i > 3) break;

                        var explicitAffix = maxrollBuild.Data.Items[item.Value].Explicits[i];
                        int affixSno = explicitAffix.Nid;
                        AffixInfo? affixInfo = _affixManager.GetAffixInfoMaxrollByIdSno(affixSno.ToString());

                        if (affixInfo == null)
                        {
                            // Only log warning when affix is not found and it's not a unique aspect.
                            // Note: Check needed because list of affixes returned by Maxroll also contains the unique aspect.
                            //       In season 11 sanctified unique aspects are also added as affixes and will log a warning here.
                            if (_affixManager.GetUniqueInfoMaxrollByIdSno(affixSno.ToString()) == null)
                            {
                                _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown affix sno: {affixSno}");
                                WeakReferenceMessenger.Default.Send(new WarningOccurredMessage(new WarningOccurredMessageParams
                                {
                                    Message = $"Imported Maxroll build contains unknown affix sno: {affixSno}."
                                }));
                            }
                        }
                        else
                        {
                            if (!affixPreset.ItemAffixes.Any(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType) && !a.IsImplicit))
                            {
                                affixPreset.ItemAffixes.Add(new ItemAffix
                                {
                                    Id = affixInfo.IdName,
                                    Type = itemType,
                                    Color = explicitAffix.Greater ? _settingsManager.Settings.DefaultColorGreater : _settingsManager.Settings.DefaultColorNormal,
                                    IsGreater = explicitAffix.Greater
                                });
                            }
                        }
                    }

                    // Add all tempered affixes for current item.Value
                    foreach (var temperedAffix in maxrollBuild.Data.Items[item.Value].Tempered)
                    {
                        int affixSno = temperedAffix.Nid;
                        AffixInfo? affixInfo = _affixManager.GetAffixInfoMaxrollByIdSno(affixSno.ToString());

                        if (affixInfo == null)
                        {
                            _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown tempered affix sno: {affixSno}");
                            WeakReferenceMessenger.Default.Send(new WarningOccurredMessage(new WarningOccurredMessageParams
                            {
                                Message = $"Imported Maxroll build contains unknown tempered affix sno: {affixSno}."
                            }));
                        }
                        else
                        {
                            if (!affixPreset.ItemAffixes.Any(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType) && a.IsTempered))
                            {
                                affixPreset.ItemAffixes.Add(new ItemAffix
                                {
                                    Id = affixInfo.IdName,
                                    Type = itemType,
                                    Color = _settingsManager.Settings.DefaultColorTempered,
                                    IsTempered = true
                                });
                            }
                        }
                    }

                    // Find all aspects / legendary powers
                    foreach (var aspect in maxrollBuild.Data.Items[item.Value].Aspects)
                    {
                        int aspectId = aspect.Nid;
                        if (aspectId != 0)
                        {
                            aspects.Add(aspectId);
                        }
                    }
                }

                // Add all aspects to preset
                foreach (var aspectSno in aspects)
                {
                    AspectInfo? aspectInfo = _affixManager.GetAspectInfoMaxrollByIdSno(aspectSno.ToString());
                    if (aspectInfo == null)
                    {
                        _logger.LogWarning($"{MethodBase.GetCurrentMethod()?.Name}: Unknown aspect sno: {aspectSno}");
                        WeakReferenceMessenger.Default.Send(new WarningOccurredMessage(new WarningOccurredMessageParams
                        {
                            Message = $"Imported Maxroll build contains unknown aspect sno: {aspectSno}."
                        }));
                    }
                    else
                    {
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectInfo.IdName, Type = Constants.ItemTypeConstants.Helm, Color = _settingsManager.Settings.DefaultColorAspects });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectInfo.IdName, Type = Constants.ItemTypeConstants.Chest, Color = _settingsManager.Settings.DefaultColorAspects });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectInfo.IdName, Type = Constants.ItemTypeConstants.Gloves, Color = _settingsManager.Settings.DefaultColorAspects });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectInfo.IdName, Type = Constants.ItemTypeConstants.Pants, Color = _settingsManager.Settings.DefaultColorAspects });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectInfo.IdName, Type = Constants.ItemTypeConstants.Boots, Color = _settingsManager.Settings.DefaultColorAspects });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectInfo.IdName, Type = Constants.ItemTypeConstants.Amulet, Color = _settingsManager.Settings.DefaultColorAspects });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectInfo.IdName, Type = Constants.ItemTypeConstants.Ring, Color = _settingsManager.Settings.DefaultColorAspects });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectInfo.IdName, Type = Constants.ItemTypeConstants.Weapon, Color = _settingsManager.Settings.DefaultColorAspects });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectInfo.IdName, Type = Constants.ItemTypeConstants.Ranged, Color = _settingsManager.Settings.DefaultColorAspects });
                        affixPreset.ItemAspects.Add(new ItemAffix { Id = aspectInfo.IdName, Type = Constants.ItemTypeConstants.Offhand, Color = _settingsManager.Settings.DefaultColorAspects });
                    }
                }

                // Add all paragon boards
                if (_settingsManager.Settings.IsImportParagonMaxrollEnabled)
                {
                    foreach (var paragonBoardStep in maxrollBuildDataProfileJson.Paragon.Steps)
                    {
                        var paragonBoards = new List<ParagonBoard>();

                        string paragonBoardStepName = paragonBoardStep.Name;
                        foreach (var paragonBoardData in paragonBoardStep.Data)
                        {
                            var paragonBoard = new ParagonBoard();
                            paragonBoard.Name = _affixManager.GetParagonBoardLocalisation(paragonBoardData.Id);
                            paragonBoard.Glyph = _affixManager.GetParagonGlyphLocalisation(paragonBoardData.Glyph);
                            string rotationInfo = paragonBoardData.Rotation == 0 ? "0°" :
                                paragonBoardData.Rotation == 1 ? "90°" :
                                paragonBoardData.Rotation == 2 ? "180°" :
                                paragonBoardData.Rotation == 3 ? "270°" : "?°";
                            paragonBoard.Rotation = rotationInfo;
                            paragonBoards.Add(paragonBoard);

                            // Process nodes
                            int rotation = paragonBoardData.Rotation;
                            foreach (var location in paragonBoardData.Nodes.Keys)
                            {
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
                        affixPreset.ParagonBoardsList.Add(paragonBoards);
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

        private string GetItemTypeFromItemId(string itemId)
        {
            List<string> itemIdParts = itemId.Split('_').ToList();
            List<string> itemTypes = new List<string>
            {
                "1HAxe",
                "1HDagger",
                "1HFocus",
                "1HMace",
                "1HScythe",
                "1HShield",
                "1HSword",
                "1HTotem",
                "1HWand",
                "2HAxe",
                "2HBow",
                "2HCrossbow",
                "2HGlaive",
                "2HMace",
                "2HPolearm",
                "2HQuarterstaff",
                "2HScythe",
                "2HStaff",
                "2HSword",
                "Amulet",
                "Boots",
                "Chest",
                "Gloves",
                "Helm",
                "Pants",
                "Ring"
            };

            for (int i = 0; i < itemIdParts.Count; i++)
            {
                if (itemTypes.Contains(itemIdParts[i]))
                {
                    return itemIdParts[i];
                }
            }

            return itemIdParts[0];
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

                    WeakReferenceMessenger.Default.Send(new MaxrollBuildsLoadedMessage());
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
