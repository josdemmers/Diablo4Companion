using D4Companion.Entities;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using FuzzierSharp;
using FuzzierSharp.SimilarityRatio;
using FuzzierSharp.SimilarityRatio.Scorer.StrategySensitive;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.Json;
using TesseractOCR.Enums;
using TesseractOCR;
using Prism.Events;
using D4Companion.Events;
using System.Text.RegularExpressions;
using System.Linq;
using D4DataParser.Entities;
using System.Collections.Concurrent;

namespace D4Companion.Services
{
    public class OcrHandler : IOcrHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private List<AffixInfo> _affixes = new List<AffixInfo>();
        private List<string> _affixDescriptions = new List<string>();
        private Dictionary<string, string> _affixMapDescriptionToId = new Dictionary<string, string>();
        private List<AspectInfo> _aspects = new List<AspectInfo>();
        private List<string> _aspectDescriptions = new List<string>();
        private Dictionary<string, string> _aspectMapDescriptionToId = new Dictionary<string, string>();
        private List<ItemTypeInfo> _itemTypes = new List<ItemTypeInfo>();
        private List<string> _itemTypesDescriptions = new List<string>();
        private Dictionary<string, string> _itemTypeMapNameToId = new Dictionary<string, string>();
        private List<SigilInfo> _sigils = new List<SigilInfo>();
        private List<string> _sigilNames = new List<string>();
        private Dictionary<string, string> _sigilMapNameToId = new Dictionary<string, string>();

        private ObjectPool<Engine> _engines;
        private Language _language = Language.English;

        // Start of Constructors region

        #region Constructors

        public OcrHandler(IEventAggregator eventAggregator, ILogger<OcrHandler> logger, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AffixLanguageChangedEvent>().Subscribe(HandleAffixLanguageChangedEvent);

            // Init logger
            _logger = logger;

            // Init services
            _settingsManager = settingsManager;

            // Init data
            InitAffixData();
            InitAspectData();
            InitItemTypeData();
            InitSigilData();

            SetLanguage();
            _engines = new ObjectPool<Engine>(() => new Engine(@"./tessdata", _language, EngineMode.Default));
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleAffixLanguageChangedEvent()
        {
            SetLanguage();

            InitAffixData();
            InitAspectData();
            InitItemTypeData();
            InitSigilData();

            // Clear OCR ObjectPool after changing language.
            _engines.Clear();
        }

        #endregion

        // Start of Methods region

        #region Methods

        /// <summary>
        /// Converts affix text to a matching AffixId
        /// </summary>
        public OcrResultAffix ConvertToAffix(string rawText)
        {
            // Note: When needed could be improve further for fuzzy search by removing non alphabetic characters.

            OcrResultAffix result = new OcrResultAffix();
            var textClean = rawText.Replace("\n", " ").Trim();
            var affixId = TextToAffix(textClean);

            result.Text = rawText;
            result.TextClean = textClean;
            result.AffixId = affixId;

            return result;
        }

        /// <summary>
        /// Converts aspect text to a matching AspectId
        /// </summary>
        public OcrResultAffix ConvertToAspect(string rawText)
        {
            // Note: When needed could be improve further for fuzzy search by removing non alphabetic characters.

            OcrResultAffix result = new OcrResultAffix();
            var textClean = rawText.Replace("\n", " ").Trim();
            var aspectId = TextToAspect(textClean);

            result.Text = rawText;
            result.TextClean = textClean;
            result.AffixId = aspectId;

            return result;
        }

        /// <summary>
        /// Finds item type in tooltip text.
        /// </summary>
        public OcrResultItemType ConvertToItemType(string rawText)
        {
            // Check for item power --> then check for item type
            // Create two possible strings
            // paralel foreach with bags
            // sort fuzzy search by score --> pick best one


            // Cases
            // (1) Item Power found.
            // (2) No Item Power found. Check for sigil at line [Count-2]
            // (3) Number of lines 1 or lower. Scrollbar active. Skip.

            OcrResultItemType result = new OcrResultItemType();
            var lines = rawText.Split(new string[] { "\n" }, StringSplitOptions.None).ToList();
            lines.RemoveAll(line => string.IsNullOrWhiteSpace(line));


            // Check if there is an item power
            int powerIndex = -1;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                string resultString = Regex.Match(lines[i], @"\d+").Value;
                // Item power 150 is the minimum value for Tier 1 items.
                if (resultString.Length >= 3 && int.Parse(resultString) >= 150)
                {
                    powerIndex = i;
                    break;
                }
            }

            // Case 1: Item with Item power. e.g. gear and aspects.
            if (powerIndex >= 0)
            {
                List<string> possibleItemTypes = new List<string>();
                if (powerIndex - 1 >= 0) possibleItemTypes.Add(lines[powerIndex - 1]);
                if (powerIndex - 2 >= 0) possibleItemTypes.Add($"{lines[powerIndex - 2].Trim()} {lines[powerIndex - 1].Trim()}");
                if (possibleItemTypes.Count == 0) return result;

                ConcurrentBag<(int, string, string, string)> itemTypeBag = new ConcurrentBag<(int, string, string, string)>();
                Parallel.ForEach(possibleItemTypes, itemType =>
                {
                    var itemTypeResult = TextToItemType(itemType);
                    itemTypeBag.Add(itemTypeResult);
                });

                // Sort results by similarity
                var itemTypes = itemTypeBag.ToList();
                itemTypes.Sort((x, y) =>
                {
                    return x.Item1 > y.Item1 ? -1 : x.Item1 < y.Item1 ? 1 : 0;
                });

                // Ignore if similarity is too low.
                if (itemTypes[0].Item1 < _settingsManager.Settings.MinimalOcrMatchType) return result;

                result.Similarity = itemTypes[0].Item1;
                result.Text = itemTypes[0].Item2;
                result.Type = itemTypes[0].Item3;
                result.TypeId = itemTypes[0].Item4;
            }
            else // Case 2: Item with no Item power. e.g. sigils.
            {
                List<string> possibleItemTypes = new List<string>();
                if (lines.Count < 2) return result;
                string possibleItemType = lines[lines.Count - 2];
                var itemTypeResult = TextToItemType(possibleItemType);

                // Ignore if similarity is too low.
                if (itemTypeResult.Item1 < _settingsManager.Settings.MinimalOcrMatchType) return result;

                result.Similarity = itemTypeResult.Item1;
                result.Text = itemTypeResult.Item2;
                result.Type = itemTypeResult.Item3;
                result.TypeId = itemTypeResult.Item4;
            }

            return result;
        }

        /// <summary>
        /// Finds power value in tooltip text.
        /// </summary>
        public OcrResult ConvertToPower(string rawText)
        {
            OcrResult result = new OcrResult();
            var lines = rawText.Split(new string[] { "\n" }, StringSplitOptions.None).ToList();
            lines.RemoveAll(line => string.IsNullOrWhiteSpace(line));

            for (int i = lines.Count - 1; i >= 0; i--)
            {
                string resultString = Regex.Match(lines[i], @"\d+").Value;
                // Item power 150 is the minimum value for Tier 1 items.
                if (resultString.Length >= 3 && int.Parse(resultString) >= 150)
                {
                    result.Text = lines[i];
                    result.TextClean = resultString;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts sigil text to a matching AffixId
        /// </summary>
        public OcrResultAffix ConvertToSigil(string rawText)
        {
            OcrResultAffix result = new OcrResultAffix();
            var textClean = rawText.Split("\n")[0];
            var affixId = TextToSigil(textClean);

            result.Text = rawText;
            result.TextClean = textClean;
            result.AffixId = affixId;

            return result;
        }

        /// <summary>
        /// Convert image to text
        /// </summary>
        public string ConvertToText(Image image)
        {
            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

            var engine = _engines.Get();
            try
            {
                using (var img = TesseractOCR.Pix.Image.LoadFromMemory(memoryStream))
                {
                    using (var page = engine.Process(img))
                    {
                        var block = page.Layout.FirstOrDefault();
                        return block?.Text ?? page.Text;
                    }
                }
            }
            finally
            {
                _engines.Return(engine);
            }
        }

        /// <summary>
        /// Convert image to text
        /// </summary>
        public string ConvertToTextUpperTooltipSection(Image image)
        {
            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

            var engine = _engines.Get();
            try
            {
                using (var img = TesseractOCR.Pix.Image.LoadFromMemory(memoryStream))
                {
                    using (var page = engine.Process(img))
                    {
                        return page.Text;
                    }
                }
            }
            finally
            {
                _engines.Return(engine);
            }
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

            // Create affix description list for FuzzierSharp
            _affixDescriptions.Clear();
            _affixDescriptions = _affixes.Select(affix => affix.DescriptionClean).ToList();

            // Create dictionary to map affix description with affix id
            _affixMapDescriptionToId.Clear();
            _affixMapDescriptionToId = _affixes.ToDictionary(affix => affix.DescriptionClean, affix => affix.IdName);
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

            // Create aspect description list for FuzzierSharp
            _aspectDescriptions.Clear();
            _aspectDescriptions = _aspects.Select(aspect => aspect.DescriptionClean).ToList();

            // Create dictionary to map aspect description with aspect id
            _aspectMapDescriptionToId.Clear();
            _aspectMapDescriptionToId = _aspects.ToDictionary(aspect => aspect.DescriptionClean, aspect => aspect.IdName);
        }

        private void InitItemTypeData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _itemTypes.Clear();
            string resourcePath = @$".\Data\ItemTypes.{language}.json";
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

                    _itemTypes = JsonSerializer.Deserialize<List<ItemTypeInfo>>(stream, options) ?? new List<ItemTypeInfo>();
                }
            }

            // Create itemtype description list for FuzzierSharp
            _itemTypesDescriptions.Clear();
            _itemTypesDescriptions = _itemTypes.Select(itemType => itemType.Name).ToList();

            // Create dictionary to map itemtype names with type id
            _itemTypeMapNameToId.Clear();
            _itemTypeMapNameToId = _itemTypes.ToDictionary(itemType => itemType.Name, itemType => itemType.Type);
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

            // Create sigil name list for FuzzierSharp
            _sigilNames.Clear();
            _sigilNames = _sigils.Select(sigil =>
            {
                string name = sigil.Name;
                if (sigil.Type.Equals("Dungeon"))
                {
                    name = $"{name} {sigil.DungeonZoneInfo}";
                }
                return name;
            }).ToList();

            // Create dictionary to map sigil name with sigil id
            _sigilMapNameToId.Clear();
            _sigilMapNameToId = _sigils.ToDictionary(sigil =>
            {
                string name = sigil.Name;
                if (sigil.Type.Equals("Dungeon"))
                {
                    name = $"{name} {sigil.DungeonZoneInfo}";
                }
                return name;
            }, sigil => sigil.IdName);
        }

        private string TextToAffix(string text)
        {
            // Notes
            // DefaultRatioScorer: Fast but does not work well with single word affixes like "Thorns". But doesn't have the TokenSetScorer issues.
            // TokenSetScorer: Picks the wrong "+#% Damage" instead of the longer "+#% Shadow Damage Over Time".
            var result = Process.ExtractOne(text, _affixDescriptions, scorer: ScorerCache.Get<DefaultRatioScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return _affixMapDescriptionToId[result.Value];
        }

        private string TextToAspect(string text)
        {
            // Notes
            // TokenSetScorer: Fastest for large amount of text like the aspect descriptions.
            var result = Process.ExtractOne(text, _aspectDescriptions, scorer: ScorerCache.Get<TokenSetScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return _aspectMapDescriptionToId[result.Value];
        }

        private (int, string, string, string) TextToItemType(string text)
        {
            var result = Process.ExtractOne(text, _itemTypesDescriptions, scorer: ScorerCache.Get<DefaultRatioScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return (result.Score, text, result.Value, _itemTypeMapNameToId[result.Value]);
        }

        private string TextToSigil(string text)
        {
            // Notes
            // WeightedRatioScorer: This is the default scorer but is bugged in some cases. See https://github.com/JakeBayer/FuzzySharp/issues/47
            var result = Process.ExtractOne(text, _sigilNames, scorer: ScorerCache.Get<TokenSetScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return _sigilMapNameToId[result.Value];
        }

        private void SetLanguage()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            switch(language)
            {
                case "deDE":
                    _language = Language.German;
                    break;
                case "enUS":
                    _language = Language.English;
                    break;
                case "esES":
                    _language = Language.SpanishCastilian;
                    break;
                case "esMX":
                    _language = Language.SpanishCastilian;
                    break;
                case "frFR":
                    _language = Language.French;
                    break;
                case "itIT":
                    _language = Language.Italian;
                    break;
                case "jaJP":
                    _language = Language.Japanese;
                    break;
                case "koKR":
                    _language = Language.Korean;
                    break;
                case "plPL":
                    _language = Language.Polish;
                    break;
                case "ptBR":
                    _language = Language.Portuguese;
                    break;
                case "ruRU":
                    _language = Language.Russian;
                    break;
                case "trTR":
                    _language = Language.Turkish;
                    break;
                case "zhCN":
                    _language = Language.ChineseSimplified;
                    break;
                case "zhTW":
                    _language = Language.ChineseTraditional;
                    break;
                default:
                    _language = Language.English;
                    break;
            }
        }

        #endregion
    }
}
