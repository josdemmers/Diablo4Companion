using D4Companion.Entities;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using FuzzierSharp;
using FuzzierSharp.SimilarityRatio;
using FuzzierSharp.SimilarityRatio.Scorer.Composite;
using FuzzierSharp.SimilarityRatio.Scorer.StrategySensitive;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.Json;
using TesseractOCR.Enums;
using TesseractOCR;

namespace D4Companion.Services
{
    // TODO: Reset data when affix language is changed.

    public class OcrHandler : IOcrHandler
    {
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private List<AffixInfo> _affixes = new List<AffixInfo>();
        private List<string> _affixDescriptions = new List<string>();
        private Dictionary<string, string> _affixMapDescriptionToId = new Dictionary<string, string>();
        private List<SigilInfo> _sigils = new List<SigilInfo>();
        private List<string> _sigilNames = new List<string>();
        private Dictionary<string, string> _sigilMapNameToId = new Dictionary<string, string>();

        private object _lock = new object();

        // Start of Constructors region

        #region Constructors

        public OcrHandler(ILogger<OcrHandler> logger, ISettingsManager settingsManager)
        {
            // Init logger
            _logger = logger;

            // Init services
            _settingsManager = settingsManager;

            // Init data
            InitAffixData();
            InitSigilData();
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

        #endregion

        // Start of Methods region

        #region Methods

        public string ConvertToAffix(Image image)
        {
            string affixId = string.Empty;

            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);

            // TODO: Make language configurable. Use affix language.
            byte[] fileBytes = memoryStream.ToArray();
            using (var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default))
            {
                using (var img = TesseractOCR.Pix.Image.LoadFromMemory(fileBytes))
                {
                    using (var page = engine.Process(img))
                    {
                        var text = page.Text;
                        text = text.Split("\n\n")[0];
                        text = text.Replace("\n", " ").Trim();
                        affixId = TextToAffix(text);
                        //lock(_lock) 
                        //{
                        //    TextToAffixTest(text);
                        //}
                    }
                }
            }
            return affixId;
        }

        public string ConvertToSigil(Image image)
        {
            string affixId = string.Empty;

            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);

            // TODO: Make language configurable. Use affix language.
            byte[] fileBytes = memoryStream.ToArray();
            using (var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default))
            {
                using (var img = TesseractOCR.Pix.Image.LoadFromMemory(fileBytes))
                {
                    using (var page = engine.Process(img))
                    {
                        var text = page.Text;
                        text = text.Split("\n")[0];
                        affixId = TextToSigil(text);
                    }
                }
            }
            return affixId;
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
            _affixDescriptions = _affixes.Select(affix => affix.Description).ToList();

            // Create dictionary to map affix description with affix id
            _affixMapDescriptionToId.Clear();
            _affixMapDescriptionToId = _affixes.ToDictionary(affix => affix.Description, affix => affix.IdName);
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
            _sigilNames = _sigils.Select(sigil => sigil.Name).ToList();

            // Create dictionary to map sigil name with sigil id
            _sigilMapNameToId.Clear();
            _sigilMapNameToId = _sigils.ToDictionary(sigil => sigil.Name, sigil => sigil.IdName);
        }

        private string TextToAffix(string text)
        {
            var result = Process.ExtractOne(text, _affixDescriptions, scorer: ScorerCache.Get<WeightedRatioScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return _affixMapDescriptionToId[result.Value];
        }

        private string TextToSigil(string text)
        {
            var result = Process.ExtractOne(text, _sigilNames, scorer: ScorerCache.Get<WeightedRatioScorer>());

            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {result}");

            return _sigilMapNameToId[result.Value];
        }

        private void TextToAffixTest(string text)
        {
            _logger.LogDebug(string.Empty);
            _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}");
            _logger.LogDebug($"Input: {text}");
            _logger.LogDebug($"---");

            // DefaultRatioScorer
            var watch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogDebug($"Scorer: DefaultRatioScorer");

            var results = Process.ExtractTop(text, _affixDescriptions, scorer: ScorerCache.Get<DefaultRatioScorer>(), limit: 3);
            foreach (var r in results)
            {
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {r}");
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"Elapsed time: {elapsedMs}");
            _logger.LogDebug($"---");

            // PartialRatioScorer
            watch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogDebug($"Scorer: PartialRatioScorer");

            results = Process.ExtractTop(text, _affixDescriptions, scorer: ScorerCache.Get<PartialRatioScorer>(), limit: 3);
            foreach (var r in results)
            {
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {r}");
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"Elapsed time: {elapsedMs}");
            _logger.LogDebug($"---");

            // TokenSetScorer
            watch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogDebug($"Scorer: TokenSetScorer");

            results = Process.ExtractTop(text, _affixDescriptions, scorer: ScorerCache.Get<TokenSetScorer>(), limit: 3);
            foreach (var r in results)
            {
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {r}");
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"Elapsed time: {elapsedMs}");
            _logger.LogDebug($"---");

            // PartialTokenSetScorer
            watch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogDebug($"Scorer: PartialTokenSetScorer");

            results = Process.ExtractTop(text, _affixDescriptions, scorer: ScorerCache.Get<PartialTokenSetScorer>(), limit: 3);
            foreach (var r in results)
            {
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {r}");
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"Elapsed time: {elapsedMs}");
            _logger.LogDebug($"---");

            // TokenSortScorer
            watch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogDebug($"Scorer: TokenSortScorer");

            results = Process.ExtractTop(text, _affixDescriptions, scorer: ScorerCache.Get<TokenSortScorer>(), limit: 3);
            foreach (var r in results)
            {
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {r}");
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"Elapsed time: {elapsedMs}");
            _logger.LogDebug($"---");

            // PartialTokenSortScorer
            watch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogDebug($"Scorer: PartialTokenSortScorer");

            results = Process.ExtractTop(text, _affixDescriptions, scorer: ScorerCache.Get<PartialTokenSortScorer>(), limit: 3);
            foreach (var r in results)
            {
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {r}");
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"Elapsed time: {elapsedMs}");
            _logger.LogDebug($"---");

            // TokenAbbreviationScorer
            watch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogDebug($"Scorer: TokenAbbreviationScorer");

            results = Process.ExtractTop(text, _affixDescriptions, scorer: ScorerCache.Get<TokenAbbreviationScorer>(), limit: 3);
            foreach (var r in results)
            {
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {r}");
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"Elapsed time: {elapsedMs}");
            _logger.LogDebug($"---");

            // PartialTokenAbbreviationScorer
            watch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogDebug($"Scorer: PartialTokenAbbreviationScorer");

            results = Process.ExtractTop(text, _affixDescriptions, scorer: ScorerCache.Get<PartialTokenAbbreviationScorer>(), limit: 3);
            foreach (var r in results)
            {
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {r}");
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"Elapsed time: {elapsedMs}");
            _logger.LogDebug($"---");

            // WeightedRatioScorer
            watch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogDebug($"Scorer: WeightedRatioScorer");

            results = Process.ExtractTop(text, _affixDescriptions, scorer: ScorerCache.Get<WeightedRatioScorer>(), limit: 3);
            foreach (var r in results)
            {
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {r}");
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"Elapsed time: {elapsedMs}");
            _logger.LogDebug($"---");

            // Default
            watch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogDebug($"Scorer: Default");

            results = Process.ExtractTop(text, _affixDescriptions, scorer: null, limit: 3);
            foreach (var r in results)
            {
                _logger.LogDebug($"{MethodBase.GetCurrentMethod()?.Name}: {r}");
            }

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;
            _logger.LogDebug($"Elapsed time: {elapsedMs}");
            _logger.LogDebug($"---");
        }

        #endregion
    }
}
