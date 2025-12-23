using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using D4Companion.Messages;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace D4Companion.Services
{
    public class AffixManager : IAffixManager
    {
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private List<AffixInfo> _affixes = new List<AffixInfo>();
        private List<AffixPreset> _affixPresets = new List<AffixPreset>();
        private List<AspectInfo> _aspects = new List<AspectInfo>();
        private List<SigilInfo> _sigils = new List<SigilInfo>();
        private List<UniqueInfo> _uniques = new List<UniqueInfo>();
        private List<RuneInfo> _runes = new List<RuneInfo>();
        private List<ParagonBoardInfo> _paragonBoards = new List<ParagonBoardInfo>();
        private List<ParagonGlyphInfo> _paragonGlyphs = new List<ParagonGlyphInfo>();
        private Dictionary<string, double> _minimalAffixValues = new Dictionary<string, double>(); // <affixId, minimalAffixValue>
        private Dictionary<string, string> _sigilDungeonTiers = new Dictionary<string, string>(); // <sigilId, tier>

        // Start of Constructors region

        #region Constructors

        public AffixManager(ILogger<AffixManager> logger, ISettingsManager settingsManager)
        {
            // Init services
            _logger = logger;
            _settingsManager = settingsManager;

            // Init messages
            WeakReferenceMessenger.Default.Register<AffixLanguageChangedMessage>(this, HandleAffixLanguageChangedMessage);
            WeakReferenceMessenger.Default.Register<ApplicationLoadedMessage>(this, HandleApplicationLoadedMessage);

            // Init store data
            InitAffixData();
            InitAffixMinimalValueData();
            InitAspectData();
            InitSigilData();
            InitSigilDungeonTierData();
            InitUniqueData();
            InitRuneData();
            InitParagonBoardData();
            InitParagonGlyphData();

            // Load affix presets
            LoadAffixPresets();
        }       

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<AffixInfo> Affixes { get => _affixes; set => _affixes = value; }
        public List<AffixPreset> AffixPresets { get => _affixPresets; }
        public List<AspectInfo> Aspects { get => _aspects; set => _aspects = value; }
        public List<SigilInfo> Sigils { get => _sigils; set => _sigils = value; }
        public List<UniqueInfo> Uniques { get => _uniques; set => _uniques = value; }
        public List<RuneInfo> Runes { get => _runes; set => _runes = value; }
        public List<ParagonBoardInfo> ParagonBoards { get => _paragonBoards; set => _paragonBoards = value; }
        public List<ParagonGlyphInfo> ParagonGlyphs { get => _paragonGlyphs; set => _paragonGlyphs = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleAffixLanguageChangedMessage(object recipient, AffixLanguageChangedMessage message)
        {
            InitAffixData();
            InitAspectData();
            InitSigilData();
            InitUniqueData();
            InitRuneData();
            InitParagonBoardData();
            InitParagonGlyphData();

            ValidateAffixPresets();
        }

        private void HandleApplicationLoadedMessage(object recipient, ApplicationLoadedMessage message)
        {
            ValidateAffixPresets();
            ValidateMultiBuild();
        }

        #endregion

        // Start of Methods region

        #region Methods

        public void AddAffixPreset(AffixPreset affixPreset)
        {
            _affixPresets.Add(affixPreset);

            // Sort list
            _affixPresets.Sort((x, y) =>
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });

            ValidateAffixPresets();

            WeakReferenceMessenger.Default.Send(new AffixPresetAddedMessage());
        }

        public void RemoveAffixPreset(AffixPreset affixPreset)
        {
            if (affixPreset == null) return;

            _affixPresets.Remove(affixPreset);

            // Sort list
            _affixPresets.Sort((x, y) =>
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });

            SaveAffixPresets();
            ValidateMultiBuild();

            WeakReferenceMessenger.Default.Send(new AffixPresetRemovedMessage());
        }

        public void AddAffix(AffixInfo affixInfo, string itemType)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            preset.ItemAffixes.Add(new ItemAffix
            {
                Id = affixInfo.IdName,
                Type = itemType,
                Color = _settingsManager.Settings.DefaultColorNormal
            });
            SaveAffixPresets();

            WeakReferenceMessenger.Default.Send(new SelectedAffixesChangedMessage());
        }

        public void RemoveAffix(ItemAffix itemAffix)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            var affix = preset.ItemAffixes.FirstOrDefault(a => a.Id.Equals(itemAffix.Id) && a.Type.Equals(itemAffix.Type) &&
                a.IsImplicit == itemAffix.IsImplicit && a.IsGreater == itemAffix.IsGreater && a.IsTempered == itemAffix.IsTempered);
            if (affix == null) return;
            
            preset.ItemAffixes.Remove(affix);
            SaveAffixPresets();

            WeakReferenceMessenger.Default.Send(new SelectedAffixesChangedMessage());
        }

        public void AddAspect(AspectInfo aspectInfo, string itemType)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (!preset.ItemAspects.Any(a => a.Id.Equals(aspectInfo.IdName) && a.Type.Equals(itemType)))
            {
                preset.ItemAspects.Add(new ItemAffix
                {
                    Id = aspectInfo.IdName,
                    Type = itemType,
                    Color = _settingsManager.Settings.DefaultColorAspects
                });
                SaveAffixPresets();
            }

            WeakReferenceMessenger.Default.Send(new SelectedAspectsChangedMessage());
        }

        public void RemoveAspect(ItemAffix itemAffix)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (preset.ItemAspects.RemoveAll(a => a.Id.Equals(itemAffix.Id)) > 0)
            {
                SaveAffixPresets();
            }

            WeakReferenceMessenger.Default.Send(new SelectedAspectsChangedMessage());
        }

        public void AddSigil(SigilInfo sigilInfo, string itemType)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (!preset.ItemSigils.Any(a => a.Id.Equals(sigilInfo.IdName) && a.Type.Equals(itemType)))
            {
                preset.ItemSigils.Add(new ItemAffix
                {
                    Id = sigilInfo.IdName,
                    Type = itemType,
                    Color = _settingsManager.Settings.SelectedSigilDisplayMode.Equals("Whitelisting") ? _settingsManager.Settings.DefaultColorNormal : Colors.Red
                });
                SaveAffixPresets();
            }

            WeakReferenceMessenger.Default.Send(new SelectedSigilsChangedMessage());
        }

        public void RemoveSigil(ItemAffix itemAffix)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (preset.ItemSigils.RemoveAll(a => a.Id.Equals(itemAffix.Id)) > 0)
            {
                SaveAffixPresets();
            }

            WeakReferenceMessenger.Default.Send(new SelectedSigilsChangedMessage());
        }

        public void AddUnique(UniqueInfo uniqueInfo)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (!preset.ItemUniques.Any(a => a.Id.Equals(uniqueInfo.IdName)))
            {
                preset.ItemUniques.Add(new ItemAffix
                {
                    Id = uniqueInfo.IdName,
                    Color = _settingsManager.Settings.DefaultColorUniques
                });
                SaveAffixPresets();
            }

            WeakReferenceMessenger.Default.Send(new SelectedUniquesChangedMessage());
        }

        public void RemoveUnique(ItemAffix itemAffix)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (preset.ItemUniques.RemoveAll(a => a.Id.Equals(itemAffix.Id)) > 0)
            {
                SaveAffixPresets();
            }

            WeakReferenceMessenger.Default.Send(new SelectedUniquesChangedMessage());
        }

        public void AddRune(RuneInfo runeInfo)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (!preset.ItemRunes.Any(a => a.Id.Equals(runeInfo.IdName)))
            {
                preset.ItemRunes.Add(new ItemAffix
                {
                    Id = runeInfo.IdName,
                    Color = _settingsManager.Settings.DefaultColorRunes
                });
                SaveAffixPresets();
            }

            WeakReferenceMessenger.Default.Send(new SelectedRunesChangedMessage());
        }

        public void RemoveRune(ItemAffix itemAffix)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (preset.ItemRunes.RemoveAll(a => a.Id.Equals(itemAffix.Id)) > 0)
            {
                SaveAffixPresets();
            }

            WeakReferenceMessenger.Default.Send(new SelectedRunesChangedMessage());
        }

        private void InitAffixData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _affixes.Clear();
            string resourcePath = @$".\Data\Affixes.{language}.json";
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
        }

        private void InitAffixMinimalValueData()
        {
            try
            {
                _minimalAffixValues.Clear();
                string resourcePath = @$".\Config\MinimalAffixValues.json";
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

                        _minimalAffixValues = JsonSerializer.Deserialize<Dictionary<string, double>>(stream, options) ?? new Dictionary<string, double> { };
                    }
                }
            }
            catch { }
        }

        private void InitAspectData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _aspects.Clear();
            string resourcePath = @$".\Data\Aspects.{language}.json";
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
        }

        private void InitSigilData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _sigils.Clear();
            string resourcePath = @$".\Data\Sigils.{language}.json";
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

                    _sigils = JsonSerializer.Deserialize<List<SigilInfo>>(stream, options) ?? new List<SigilInfo>();
                }
            }
        }

        private void InitSigilDungeonTierData()
        {
            try
            {
                _sigilDungeonTiers.Clear();
                string resourcePath = @$".\Config\DungeonTiers.json";
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

                        _sigilDungeonTiers = JsonSerializer.Deserialize<Dictionary<string, string>>(stream, options) ?? new Dictionary<string, string> { };
                    }
                }
            }
            catch { }
        }

        private void InitUniqueData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _uniques.Clear();
            string resourcePath = @$".\Data\Uniques.{language}.json";
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
        }

        private void InitRuneData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _runes.Clear();
            string resourcePath = @$".\Data\Runes.{language}.json";
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
        }

        private void InitParagonBoardData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _paragonBoards.Clear();
            string resourcePath = @$".\Data\ParagonBoards.{language}.json";
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

                    _paragonBoards = JsonSerializer.Deserialize<List<ParagonBoardInfo>>(stream, options) ?? new List<ParagonBoardInfo>();
                }
            }
        }

        private void InitParagonGlyphData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _paragonGlyphs.Clear();
            string resourcePath = @$".\Data\ParagonGlyphs.{language}.json";
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

                    _paragonGlyphs = JsonSerializer.Deserialize<List<ParagonGlyphInfo>>(stream, options) ?? new List<ParagonGlyphInfo>();
                }
            }
        }

        public ItemAffix GetAffix(string affixId, string affixType, string itemType)
        {
            var affixDefault = new ItemAffix
            {
                Id = affixId,
                Type = itemType,
                Color = Colors.Red
            };

            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return affixDefault;

            bool isImplicit = affixType.Equals(Constants.AffixTypeConstants.Implicit);
            bool isTempered = affixType.Equals(Constants.AffixTypeConstants.Tempered);
            var affix = preset.ItemAffixes.FirstOrDefault(a => a.Id.Equals(affixId) && a.Type.Equals(itemType) && a.IsImplicit == isImplicit && a.IsTempered == isTempered);

            // Check if the affix is set to accept any item type.
            if (affix == null)
            {
                affix = preset.ItemAffixes.FirstOrDefault(a => a.Id.Equals(affixId));
                affix = affix?.IsAnyType ?? false ? affix : null;
            }

            if (affix == null) return affixDefault;
            return affix;
        }

        public string GetAffixDescription(string affixId)
        {
            var affixInfo = _affixes.FirstOrDefault(a => a.IdName.Equals(affixId));
            if (affixInfo != null)
            {
                return affixInfo.Description;
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetAffixId(string affixSno)
        {
            var affixInfo = _affixes.FirstOrDefault(a => a.IdSno.Equals(affixSno));
            if (affixInfo != null)
            {
                return affixInfo.IdName;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Find Affix with matching sno for affixes used by imported Maxroll builds.
        /// Uses an affix list contaning all known affixes, included affixes with duplicated descriptions.
        /// </summary>
        /// <param name="affixIdSno"></param>
        /// <returns></returns>
        public AffixInfo? GetAffixInfoMaxrollByIdSno(string affixIdSno)
        {
            return _affixes.FirstOrDefault(a => a.IdSnoList.Contains(affixIdSno));
        }

        /// <summary>
        /// Find Affix with matching name for affixes used by imported Maxroll builds.
        /// Uses an affix list contaning all known affixes, included affixes with duplicated descriptions.
        /// </summary>
        /// <param name="affixIdName"></param>
        /// <returns></returns>
        public AffixInfo? GetAffixInfoMaxrollByIdName(string affixIdName)
        {
            return _affixes.FirstOrDefault(a => a.IdNameList.Contains(affixIdName));
        }

        public double GetAffixMinimalValue(string idName)
        {
            return _minimalAffixValues.TryGetValue(idName, out var minimalValue) ? minimalValue : 0;
        }

        public ItemAffix GetAspect(string aspectId, string itemType)
        {
            var affixDefault = new ItemAffix
            {
                Id = aspectId,
                Type = itemType,
                Color = Colors.Red
            };

            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return affixDefault;

            var aspect = preset.ItemAspects.FirstOrDefault(a => a.Id.Equals(aspectId) && a.Type.Equals(itemType));
            if (aspect == null) return affixDefault;
            return aspect;
        }

        public string GetAspectDescription(string aspectId)
        {
            var aspectInfo = _aspects.FirstOrDefault(a => a.IdName.Equals(aspectId));
            if (aspectInfo != null)
            {
                return aspectInfo.Description;
            }
            else
            {
                return string.Empty;
            }
        }

        //public string GetAspectId(int aspectSno)
        //{
        //    var aspectInfo = _aspects.FirstOrDefault(a => a.IdSno.Equals(aspectSno));
        //    if (aspectInfo != null)
        //    {
        //        return aspectInfo.IdName;
        //    }
        //    else
        //    {
        //        return string.Empty;
        //    }
        //}

        public string GetAspectName(string aspectId)
        {
            var aspectInfo = _aspects.FirstOrDefault(a => a.IdName.Equals(aspectId));
            if (aspectInfo != null)
            {
                return aspectInfo.Name;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Find Aspect with matching sno for aspects used by imported Maxroll builds.
        /// Uses an aspect list containing all known aspects, included aspects with duplicated descriptions.
        /// </summary>
        /// <param name="aspectIdSno"></param>
        /// <returns></returns>
        public AspectInfo? GetAspectInfoMaxrollByIdSno(string aspectIdSno)
        {
            return _aspects.FirstOrDefault(a => a.IdSnoList.Contains(aspectIdSno));
        }

        /// <summary>
        /// Find Aspect with matching name for aspects used by imported Maxroll builds.
        /// Uses an aspect list contaning all known aspects, included aspects with duplicated descriptions.
        /// </summary>
        /// <param name="aspectIdName"></param>
        /// <returns></returns>
        public AspectInfo? GetAspectInfoMaxrollByIdName(string aspectIdName)
        {
            return _aspects.FirstOrDefault(a => a.IdNameList.Contains(aspectIdName));
        }

        public string GetParagonBoardLocalisation(string id)
        {
            return _paragonBoards.FirstOrDefault(board => board.IdName.Equals(id,StringComparison.OrdinalIgnoreCase))?.Name ?? id;
        }

        public string GetParagonGlyphLocalisation(string id)
        {
            return _paragonGlyphs.FirstOrDefault(board => board.IdName.Equals(id, StringComparison.OrdinalIgnoreCase))?.Name ?? id;
        }

        public ItemAffix GetSigil(string affixId, string itemType)
        {
            var affixDefault = new ItemAffix
            {
                Id = affixId,
                Type = itemType,
                Color = _settingsManager.Settings.SelectedSigilDisplayMode.Equals("Whitelisting") ? Colors.Red : _settingsManager.Settings.DefaultColorNormal
            };

            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return affixDefault;

            var affix = preset.ItemSigils.FirstOrDefault(a => a.Id.Equals(affixId) && a.Type.Equals(itemType));
            if (affix == null) return affixDefault;
            return affix;
        }

        public string GetSigilDescription(string sigilId)
        {
            var sigilInfo = _sigils.FirstOrDefault(a => a.IdName.Equals(sigilId));
            if (sigilInfo != null)
            {
                return sigilInfo.Description;
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetSigilDungeonTier(string sigilId)
        {
            return _sigilDungeonTiers.TryGetValue(sigilId, out var tier) ? tier : "F";
        }

        public string GetSigilType(string sigilId)
        {
            var sigilInfo = _sigils.FirstOrDefault(a => a.IdName.Equals(sigilId));
            if (sigilInfo != null)
            {
                return sigilInfo.Type;
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetSigilName(string sigilId)
        {
            var sigilInfo = _sigils.FirstOrDefault(a => a.IdName.Equals(sigilId));
            if (sigilInfo != null)
            {
                return sigilInfo.Name;
            }
            else
            {
                return string.Empty;
            }
        }

        public ItemAffix GetUnique(string uniqueId, string itemType)
        {
            var affixDefault = new ItemAffix
            {
                Id = uniqueId,
                Type = itemType,
                Color = Colors.Red
            };

            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return affixDefault;

            var unique = preset.ItemUniques.FirstOrDefault(a => a.Id.Equals(uniqueId));
            if (unique == null) return affixDefault;
            return unique;
        }

        public string GetUniqueDescription(string uniqueId)
        {
            var uniqueInfo = _uniques.FirstOrDefault(a => a.IdName.Equals(uniqueId));
            if (uniqueInfo != null)
            {
                return uniqueInfo.Description;
            }
            else
            {
                return string.Empty;
            }
        }

        public UniqueInfo? GetUniqueInfoMaxrollByIdSno(string idSno)
        {
            return _uniques.FirstOrDefault(a => a.IdSno.Contains(idSno));
        }

        public string GetUniqueName(string uniqueId)
        {
            //var uniqueInfo = _uniques.FirstOrDefault(a => a.IdName.Contains(uniqueId));
            var uniqueInfo = _uniques.FirstOrDefault(a => a.IdName.Equals(uniqueId));
            if (uniqueInfo != null)
            {
                return uniqueInfo.Name;
            }
            else
            {
                return string.Empty;
            }
        }

        public ItemAffix GetRune(string runeId, string itemType)
        {
            var affixDefault = new ItemAffix
            {
                Id = runeId,
                Type = itemType,
                Color = Colors.Red
            };

            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return affixDefault;

            var rune = preset.ItemRunes.FirstOrDefault(a => a.Id.Equals(runeId));
            if (rune == null) return affixDefault;
            return rune;
        }

        public string GetRuneDescription(string runeId)
        {
            var runeInfo = _runes.FirstOrDefault(a => a.IdName.Equals(runeId));
            if (runeInfo != null)
            {
                return runeInfo.Description;
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetRuneName(string runeId)
        {
            var runeInfo = _runes.FirstOrDefault(a => a.IdName.Equals(runeId));
            if (runeInfo != null)
            {
                return runeInfo.Name;
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetGearOrSigilAffixDescription(string affixId)
        {
            var affixInfo = _affixes.FirstOrDefault(a => a.IdName.Equals(affixId));
            var sigilInfo = _sigils.FirstOrDefault(a => a.IdName.Equals(affixId));
            var runeInfo = _runes.FirstOrDefault(a => a.IdName.Equals(affixId));
            if (affixInfo != null) return affixInfo.Description;
            if (sigilInfo != null) return sigilInfo.Name;
            if (runeInfo != null) return runeInfo.Description;

            return string.Empty;
        }

        public bool IsDuplicate(ItemAffix itemAffix)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return false;

            return preset.ItemAffixes.Count(affix =>
                affix.Type.Equals(itemAffix.Type) &&
                affix.Id.Equals(itemAffix.Id) &&
                affix.IsGreater.Equals(itemAffix.IsGreater) &&
                affix.IsTempered.Equals(itemAffix.IsTempered) &&
                affix.IsImplicit.Equals(itemAffix.IsImplicit)) > 1;
        }

        public void ResetMinimalAffixValues()
        {
            _minimalAffixValues.Clear();
            SaveAffixMinimalValueData();
        }

        public void SaveAffixColor(ItemAffix itemAffix)
        {
            SaveAffixPresets();

            WeakReferenceMessenger.Default.Send(new SelectedAffixesChangedMessage());
            WeakReferenceMessenger.Default.Send(new SelectedAspectsChangedMessage());
            WeakReferenceMessenger.Default.Send(new SelectedSigilsChangedMessage());
            WeakReferenceMessenger.Default.Send(new SelectedUniquesChangedMessage());
            WeakReferenceMessenger.Default.Send(new SelectedRunesChangedMessage());
        }

        private void LoadAffixPresets()
        {
            _affixPresets.Clear();

            string fileName = "Config/AffixPresets-v2.json";
            if (File.Exists(fileName))
            {
                using FileStream stream = File.OpenRead(fileName);
                _affixPresets = JsonSerializer.Deserialize<List<AffixPreset>>(stream) ?? new List<AffixPreset>();
            }

            // Sort list
            _affixPresets.Sort((x, y) =>
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });

            SaveAffixPresets();
        }

        private void ValidateAffixPresets()
        {
            foreach (AffixPreset preset in _affixPresets)
            {
                // Affixes
                foreach (var affix in preset.ItemAffixes)
                {
                    var affixInfo = _affixes.FirstOrDefault(a => a.IdName.Equals(affix.Id));
                    if (affixInfo == null)
                    {
                        List<string> affixIds = affix.Id.Split(';').ToList();
                        int bestMatch = 0;
                        string newAffixId = string.Empty;

                        foreach (var affixInfoItem in _affixes)
                        {
                            int match = affixInfoItem.IdNameList.Where(a => affixIds.Contains(a)).Count();
                            if (match > bestMatch)
                            {
                                bestMatch = match;
                                newAffixId = affixInfoItem.IdName;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(newAffixId))
                        {
                            WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                            {
                                Message = $"Build: \"{preset.Name}\": Affix not found. Replace missing affix or import build again."
                            }));
                        }
                        else
                        {
                            WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                            {
                                Message = $"Build: \"{preset.Name}\": Affix not found. Replaced \"{affix.Id}\"."
                            }));

                            affix.Id = newAffixId;
                        }
                    }
                }

                // Uniques
                foreach (var unique in preset.ItemUniques)
                {
                    var uniqueInfo = _uniques.FirstOrDefault(a => a.IdName.Equals(unique.Id));
                    if (uniqueInfo == null)
                    {
                        List<string> uniqueIds = unique.Id.Split(';').ToList();
                        int bestMatch = 0;
                        string newUniqueId = string.Empty;

                        foreach (var uniqueInfoItem in _uniques)
                        {
                            int match = uniqueInfoItem.IdNameList.Where(a => uniqueIds.Contains(a)).Count();
                            if (match > bestMatch)
                            {
                                bestMatch = match;
                                newUniqueId = uniqueInfoItem.IdName;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(newUniqueId))
                        {
                            WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                            {
                                Message = $"Build: \"{preset.Name}\": Unique not found. Replace missing unique or import build again."
                            }));
                        }
                        else
                        {
                            WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                            {
                                Message = $"Build: \"{preset.Name}\": Unique not found. Replaced by \"{unique.Id}\"."
                            }));

                            unique.Id = newUniqueId;
                        }
                    }
                }
            }

            SaveAffixPresets();
        }

        public void RenamePreset(string oldName, string newName)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(oldName));
            if (preset == null) return;

            preset.Name = newName;
            SaveAffixPresets();
            _settingsManager.Settings.SelectedAffixPreset = newName;
            _settingsManager.SaveSettings();
        }

        public void SaveAffixPresets()
        {
            // Sort affixes
            foreach (var affixPreset in _affixPresets)
            {
                affixPreset.ItemAffixes.Sort((x, y) =>
                {
                    if (x.Id == y.Id && x.IsImplicit == y.IsImplicit && x.IsTempered == y.IsTempered) return 0;

                    int result = x.IsTempered && !y.IsTempered ? 1 : y.IsTempered && !x.IsTempered ? -1 : 0;
                    if (result == 0)
                    {
                        result = x.IsImplicit && !y.IsImplicit ? -1 : y.IsImplicit && !x.IsImplicit ? 1 : 0;
                    }
                    if (result == 0)
                    {
                        result = x.Id.CompareTo(y.Id);
                    }

                    return result;
                });
            }

            string fileName = "Config/AffixPresets-v2.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, AffixPresets, options);
        }

        private void SaveAffixMinimalValueData()
        {
            string fileName = "./Config/MinimalAffixValues.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, _minimalAffixValues, options);
        }

        private void SaveSigilDungeonTierData()
        {
            string fileName = "./Config/DungeonTiers.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, _sigilDungeonTiers, options);
        }

        public void SetAffixMinimalValue(string idName, double minimalValue)
        {
            _minimalAffixValues[idName] = minimalValue;

            SaveAffixMinimalValueData();
        }

        public void SetSigilDungeonTier(SigilInfo sigilInfo, string tier)
        {
            _sigilDungeonTiers[sigilInfo.IdName] = tier;

            SaveSigilDungeonTierData();
        }

        public void SetIsAnyType(ItemAffix itemAffix, bool isAnyType)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            var affixes = preset.ItemAffixes.FindAll(a => a.Id.Equals(itemAffix.Id));
            foreach ( var affix in affixes )
            {
                affix.IsAnyType = isAnyType;
            }
        }

        private void ValidateMultiBuild()
        {
            foreach (MultiBuild multiBuild in _settingsManager.Settings.MultiBuildList)
            {
                var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(multiBuild.Name));
                if (preset == null)
                {
                    WeakReferenceMessenger.Default.Send(new ErrorOccurredMessage(new ErrorOccurredMessageParams
                    {
                        Message = $"Multi build #{multiBuild.Index + 1} not found: {multiBuild.Name}."
                    }));
                }
            }
        }

        #endregion
    }
}
