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
        private List<UniqueInfo> _uniques = new List<UniqueInfo>();
        private List<string> _uniqueDescriptions = new List<string>();
        private Dictionary<string, string> _uniqueMapDescriptionToId = new Dictionary<string, string>();
        private List<RuneInfo> _runes = new List<RuneInfo>();
        private List<string> _runeDescriptions = new List<string>();
        private Dictionary<string, string> _runeMapDescriptionToId = new Dictionary<string, string>();
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
            InitUniqueData();
            InitRuneData();
            InitSigilData();
            InitItemTypeData();

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
            InitUniqueData();
            InitRuneData();
            InitSigilData();
            InitItemTypeData();

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
            OcrResultAffix result = new OcrResultAffix();
            var textClean = rawText.Replace("\n", " ");
            textClean = String.Concat(textClean.Where(c =>
                        (c < '0' || c > '9') &&
                        (c != '[') &&
                        (c != ']') &&
                        (c != '(') &&
                        (c != ')') &&
                        (c != '+') &&
                        (c != '-') &&
                        (c != '.') &&
                        (c != ',') &&
                        (c != '%'))).Trim();

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
        /// Converts unique aspect text to a matching AspectId
        /// </summary>
        public OcrResultAffix ConvertToUnique(string rawText)
        {
            // Note: When needed could be improve further for fuzzy search by removing non alphabetic characters.

            OcrResultAffix result = new OcrResultAffix();
            var textClean = rawText.Replace("\n", " ").Trim();
            var aspectId = TextToUnique(textClean);

            result.Text = rawText;
            result.TextClean = textClean;
            result.AffixId = aspectId;

            return result;
        }

        /// <summary>
        /// Converts rune text to a matching AffixId
        /// </summary>
        public OcrResultAffix ConvertToRune(string rawText)
        {
            OcrResultAffix result = new OcrResultAffix();
            var lines = rawText.Split("\n\n")[0].Split("\n").ToList();
            lines.RemoveAll(line => string.IsNullOrWhiteSpace(line));

            List<string> possibleRunes = new List<string>();
            if (lines.Count >= 1) possibleRunes.Add(lines[0]);
            if (lines.Count >= 2) possibleRunes.Add($"{lines[0].Trim()} {lines[1].Trim()}");
            if (lines.Count >= 3) possibleRunes.Add($"{lines[0].Trim()} {lines[1].Trim()} {lines[2].Trim()}");

            if (possibleRunes.Count == 0) return result;

            ConcurrentBag<(int, string, string, string)> runeBag = new ConcurrentBag<(int, string, string, string)>();
            Parallel.ForEach(possibleRunes, runeText =>
            {
                var runeResult = TextToRune(runeText);
                runeBag.Add(runeResult);
            });

            // Sort results by similarity
            var runes = runeBag.ToList();
            runes.Sort((x, y) =>
            {
                return x.Item1 > y.Item1 ? -1 : x.Item1 < y.Item1 ? 1 :
                    x.Item2.Length > y.Item2.Length ? -1 : x.Item2.Length < y.Item2.Length ? 1 : 0;
            });

            // Ignore if similarity is too low.
            if (runes[0].Item1 < _settingsManager.Settings.MinimalOcrMatchType) return result;

            result.Text = rawText;
            result.TextClean = runes[0].Item2;
            result.AffixId = runes[0].Item4;

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
            // (2) No Item Power found. Check for sigil at line [Count-2]. Check for rune at line [Count-2]
            // (3) Number of lines 1 or lower. Scrollbar active. Skip.

            OcrResultItemType result = new OcrResultItemType();
            var lines = rawText.Split(new string[] { "\n" }, StringSplitOptions.None).ToList();
            lines.RemoveAll(line => string.IsNullOrWhiteSpace(line));
            lines.RemoveAll(line =>
            {
                // Remove possible artifacts caused by the image in the top right corner of the item tooltips.
                bool hasArtifacts = line.Split(' ',StringSplitOptions.RemoveEmptyEntries).All(s => s.Length < 4);
                return hasArtifacts;
            });

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
            else // Case 2: Item with no Item power. e.g. sigils and runes.
            {
                List<string> possibleItemTypes = new List<string>();
                // Sigil
                if (lines.Count >= 2) possibleItemTypes.Add(lines[lines.Count - 2]);
                if (lines.Count >= 3) possibleItemTypes.Add($"{lines[lines.Count - 3].Trim()} {lines[lines.Count - 2].Trim()}");

                // Runes
                if (lines.Count >= 1) possibleItemTypes.Add(lines[lines.Count - 1]);
                if (lines.Count >= 2) possibleItemTypes.Add($"{lines[lines.Count - 2].Trim()} {lines[lines.Count - 1].Trim()}" );

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
                        List<(string text, Rect? boundingBox)> lines = new List<(string text, Rect? boundingBox)>();

                        foreach (var block in page.Layout)
                        {
                            foreach (var paragraph in block.Paragraphs)
                            {
                                foreach (var textLine in paragraph.TextLines)
                                {
                                    lines.Add((textLine.Text, textLine.BoundingBox));
                                }
                            }
                        }

                        if (lines.Count > 0)
                        {
                            // Check the x-position of each line to remove right aligned text, e.g, Requires Level.
                            int startPosText = lines[0].boundingBox?.X1 ?? 0;
                            int xPosOffset = 20;

                            lines.RemoveAll(line =>
                            {
                                return line.boundingBox != null && line.boundingBox.Value.X1 > startPosText + xPosOffset;
                            });
                            return string.Join(' ', lines.Select(line => line.text));
                        }

                        return page.Text;
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
                    // PageSegMode.OsdOnly: Orientation and script detection only.
                    // PageSegMode.SingleBlockVertText: Assume a single uniform block of vertically aligned text.
                    // PageSegMode.SingleLine: Treat the image as a single text line. 
                    // PageSegMode.SingleWord: Treat the image as a single word. 
                    // PageSegMode.CircleWord: Treat the image as a single word in a circle. 
                    // PageSegMode.SingleChar: Treat the image as a single character.
                    // PageSegMode.RawLine: Treat the image as a single text line, bypassing hacks that are Tesseract-specific.
                    // PageSegMode.Count: Number of enum entries.
                    // - Layout not relevant

                    // PageSegMode.AutoOsd: Automatic page segmentation with orientation and script detection. (OSD)
                    // PageSegMode.AutoOnly: Automatic page segmentation, but no OSD, or OCR.
                    // PageSegMode.Auto: Fully automatic page segmentation, but no OSD.
                    // - Has issues with smaller tooltips. Caused by differences in layout of game background and tooltip. Processes only first line.

                    // PageSegMode.SingleColumn: Assume a single column of text of variable sizes. 
                    // - Has issues with smaller tooltips. Caused by differences in layout of game background and tooltip. Empty result.

                    // PageSegMode.SingleBlock: Assume a single uniform block of text. (Default.) 
                    // - Working. 130+ ms (1080p)

                    // PageSegMode.SparseText: Find as much text as possible in no particular order. 
                    // - Working. 100+ ms (1080p)

                    // PageSegMode.SparseTextOsd: Sparse text with orientation and script det.
                    // - Working. 80+ ms (1080p)

                    // Note: PageSegMode can be simplefied when the tooltip area can be detected more precisely.
                    // As long parts of the game background is visible above the tooltip a more general approach is needed.
                    using (var page = engine.Process(img, PageSegMode.SparseTextOsd))
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

            // Create unique aspect description list for FuzzierSharp
            _uniqueDescriptions.Clear();
            _uniqueDescriptions = _uniques.Select(unique => unique.DescriptionClean).ToList();

            // Create dictionary to map unique aspect description with aspect id
            _uniqueMapDescriptionToId.Clear();
            //_uniqueMapDescriptionToId = _uniques.ToDictionary(unique => unique.DescriptionClean, unique => unique.IdName);
            foreach (var unique in _uniques)
            {
                if (!_uniqueMapDescriptionToId.ContainsKey(unique.DescriptionClean))
                {
                    _uniqueMapDescriptionToId.Add(unique.DescriptionClean, unique.IdName);
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

            // Create runes description list for FuzzierSharp
            _runeDescriptions.Clear();
            _runeDescriptions = _runes.Select(rune => rune.DescriptionClean).ToList();

            // Create dictionary to map rune description with rune id
            _runeMapDescriptionToId.Clear();
            _runeMapDescriptionToId = _runes.ToDictionary(rune => rune.DescriptionClean, rune => rune.IdName);
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
                if (sigil.Type.Equals(Constants.SigilTypeConstants.Dungeon))
                {
                    name = $"{name} {sigil.DungeonZoneInfo}";
                }
                return name;
            }).ToList();

            // Create dictionary to map sigil name with sigil id
            _sigilMapNameToId.Clear();
            //_sigilMapNameToId = _sigils.ToDictionary(sigil =>
            //{
            //    string name = sigil.Name;
            //    if (sigil.Type.Equals(Constants.SigilTypeConstants.Dungeon))
            //    {
            //        name = $"{name} {sigil.DungeonZoneInfo}";
            //    }
            //    return name;
            //}, sigil => sigil.IdName);

            // Use foreach instead because of duplicate names in some languages
            foreach (var sigil in _sigils)
            {
                string name = sigil.Name;
                if (sigil.Type.Equals(Constants.SigilTypeConstants.Dungeon))
                {
                    name = $"{name} {sigil.DungeonZoneInfo}";
                }

                if (!_sigilMapNameToId.ContainsKey(name))
                {
                    _sigilMapNameToId.Add(name, sigil.IdName);
                }
            }
        }

        private string TextToAffix(string text)
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;
            bool disablePreprocessor = language.Equals("zhCN") || language.Equals("zhTW");

            // Notes
            // DefaultRatioScorer: Fast but does not work well with single word affixes like "Thorns". But doesn't have the TokenSetScorer issues.
            // TokenSetScorer: Picks the wrong "+#% Damage" instead of the longer "+#% Shadow Damage Over Time".
            var result = disablePreprocessor ?
                Process.ExtractOne(text, _affixDescriptions, processor: (s) => s, scorer: ScorerCache.Get<DefaultRatioScorer>()) :
                Process.ExtractOne(text, _affixDescriptions, scorer: ScorerCache.Get<DefaultRatioScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return _affixMapDescriptionToId[result.Value];
        }

        private string TextToAspect(string text)
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;
            bool disablePreprocessor = language.Equals("zhCN") || language.Equals("zhTW");

            // Notes
            // TokenSetScorer: Fastest for large amount of text like the aspect descriptions.
            var result = disablePreprocessor ?
                Process.ExtractOne(text, _aspectDescriptions, processor: (s) => s, scorer: ScorerCache.Get<TokenSetScorer>()) :
                Process.ExtractOne(text, _aspectDescriptions, scorer: ScorerCache.Get<TokenSetScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return _aspectMapDescriptionToId[result.Value];
        }

        private string TextToUnique(string text)
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;
            bool disablePreprocessor = language.Equals("zhCN") || language.Equals("zhTW");

            // Notes
            // TokenSetScorer: Fastest for large amount of text like the aspect descriptions.
            var result = disablePreprocessor ?
                Process.ExtractOne(text, _uniqueDescriptions, processor: (s) => s, scorer: ScorerCache.Get<TokenSetScorer>()) :
                Process.ExtractOne(text, _uniqueDescriptions, scorer: ScorerCache.Get<TokenSetScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return _uniqueMapDescriptionToId[result.Value];
        }

        private (int, string, string, string) TextToRune(string text)
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;
            bool disablePreprocessor = language.Equals("zhCN") || language.Equals("zhTW");

            // Notes
            // TokenSetScorer: Fastest for large amount of text like the aspect descriptions.
            var result = disablePreprocessor ?
                Process.ExtractOne(text, _runeDescriptions, processor: (s) => s, scorer: ScorerCache.Get<TokenSetScorer>()) :
                Process.ExtractOne(text, _runeDescriptions, scorer: ScorerCache.Get<TokenSetScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return (result.Score, text, result.Value, _runeMapDescriptionToId[result.Value]);
        }

        private string TextToSigil(string text)
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;
            bool disablePreprocessor = language.Equals("zhCN") || language.Equals("zhTW");

            // Notes
            // WeightedRatioScorer: This is the default scorer but is bugged in some cases. See https://github.com/JakeBayer/FuzzySharp/issues/47
            var result = disablePreprocessor ?
                Process.ExtractOne(text, _sigilNames, processor: (s) => s, scorer: ScorerCache.Get<TokenSetScorer>()) :
                Process.ExtractOne(text, _sigilNames, scorer: ScorerCache.Get<TokenSetScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return _sigilMapNameToId[result.Value];
        }

        private (int, string, string, string) TextToItemType(string text)
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;
            bool disablePreprocessor = language.Equals("zhCN") || language.Equals("zhTW");

            var result = disablePreprocessor ?
                Process.ExtractOne(text, _itemTypesDescriptions, processor: (s) => s, scorer: ScorerCache.Get<DefaultRatioScorer>()):
                Process.ExtractOne(text, _itemTypesDescriptions, scorer: ScorerCache.Get<DefaultRatioScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return (result.Score, text, result.Value, _itemTypeMapNameToId[result.Value]);
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
