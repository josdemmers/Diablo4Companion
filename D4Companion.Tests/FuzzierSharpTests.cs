using D4Companion.Entities;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using D4Companion.Services;
using FuzzierSharp.SimilarityRatio.Scorer.StrategySensitive;
using FuzzierSharp.SimilarityRatio;
using System.Text.Json;
using FuzzierSharp;
using FuzzierSharp.SimilarityRatio.Scorer.Composite;

namespace D4Companion.Tests
{
    public class FuzzierSharpTests
    {
        // enUS
        private List<AffixInfo> _affixes = new List<AffixInfo>();
        private List<SigilInfo> _sigils = new List<SigilInfo>();
        private List<string> _affixDescriptions = new List<string>();
        private List<string> _sigilNames = new List<string>();
        private Dictionary<string, string> _affixMapDescriptionToId = new Dictionary<string, string>();
        private Dictionary<string, string> _sigilMapNameToId = new Dictionary<string, string>();
        private Dictionary<string, string> _affixTestMappings = new Dictionary<string, string>();
        private Dictionary<string, string> _sigilTestMappings = new Dictionary<string, string>();

        // zhCN
        private List<AffixInfo> _affixeszhCN = new List<AffixInfo>();
        private List<string> _affixDescriptionszhCN = new List<string>();
        private Dictionary<string, string> _affixMapDescriptionToIdzhCN = new Dictionary<string, string>();
        private Dictionary<string, string> _affixTestMappingszhCN = new Dictionary<string, string>();



        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Init data
            InitAffixData();
            InitAffixDatazhCN();
            InitSigilData();
            InitTestData();
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void DefaultRatioScorerTest()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappings)
                {
                    var result = Process.ExtractOne(affixTest.Key, _affixDescriptions, scorer: ScorerCache.Get<DefaultRatioScorer>());
                    var affixId = _affixMapDescriptionToId[result.Value];

                    Assert.That(affixId, Is.EqualTo(affixTest.Value), $"Input: {affixTest.Key}");
                }

                foreach (var sigilTest in _sigilTestMappings)
                {
                    var result = Process.ExtractOne(sigilTest.Key, _sigilNames, scorer: ScorerCache.Get<DefaultRatioScorer>());
                    var sigilId = _sigilMapNameToId[result.Value];

                    Assert.That(sigilId, Is.EqualTo(sigilTest.Value), $"Input: {sigilTest.Key}");
                }
            });
        }

        [Test]
        public void DefaultRatioScorerTestzhCN()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappingszhCN)
                {
                    var result = Process.ExtractOne(affixTest.Key, _affixDescriptionszhCN, processor: (s) => s, scorer: ScorerCache.Get<DefaultRatioScorer>());
                    var affixId = _affixMapDescriptionToIdzhCN[result.Value];

                    Assert.That(affixId, Is.EqualTo(affixTest.Value), $"Input: {affixTest.Key}");
                }
            });
        }

        [Test]
        public void PartialRatioScorerTest()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappings)
                {
                    var result = Process.ExtractOne(affixTest.Key, _affixDescriptions, scorer: ScorerCache.Get<PartialRatioScorer>());
                    var affixId = _affixMapDescriptionToId[result.Value];

                    Assert.That(affixId, Is.EqualTo(affixTest.Value), $"Input: {affixTest.Key}");
                }

                foreach (var sigilTest in _sigilTestMappings)
                {
                    var result = Process.ExtractOne(sigilTest.Key, _sigilNames, scorer: ScorerCache.Get<PartialRatioScorer>());
                    var sigilId = _sigilMapNameToId[result.Value];

                    Assert.That(sigilId, Is.EqualTo(sigilTest.Value), $"Input: {sigilTest.Key}");
                }
            });
        }

        [Test]
        public void TokenSetScorerTest()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappings)
                {
                    var result = Process.ExtractOne(affixTest.Key, _affixDescriptions, scorer: ScorerCache.Get<TokenSetScorer>());
                    var affixId = _affixMapDescriptionToId[result.Value];

                    Assert.That(affixId, Is.EqualTo(affixTest.Value));
                }

                foreach (var sigilTest in _sigilTestMappings)
                {
                    var result = Process.ExtractOne(sigilTest.Key, _sigilNames, scorer: ScorerCache.Get<TokenSetScorer>());
                    var sigilId = _sigilMapNameToId[result.Value];

                    Assert.That(sigilId, Is.EqualTo(sigilTest.Value), $"Input: {sigilTest.Key}");
                }
            });
        }

        [Test]
        public void PartialTokenSetScorerTest()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappings)
                {
                    var result = Process.ExtractOne(affixTest.Key, _affixDescriptions, scorer: ScorerCache.Get<PartialTokenSetScorer>());
                    var affixId = _affixMapDescriptionToId[result.Value];

                    Assert.That(affixId, Is.EqualTo(affixTest.Value), $"Input: {affixTest.Key}");
                }

                foreach (var sigilTest in _sigilTestMappings)
                {
                    var result = Process.ExtractOne(sigilTest.Key, _sigilNames, scorer: ScorerCache.Get<PartialTokenSetScorer>());
                    var sigilId = _sigilMapNameToId[result.Value];

                    Assert.That(sigilId, Is.EqualTo(sigilTest.Value), $"Input: {sigilTest.Key}");
                }
            });
        }

        [Test]
        public void TokenSortScorerTest()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappings)
                {
                    var result = Process.ExtractOne(affixTest.Key, _affixDescriptions, scorer: ScorerCache.Get<TokenSortScorer>());
                    var affixId = _affixMapDescriptionToId[result.Value];

                    Assert.That(affixId, Is.EqualTo(affixTest.Value), $"Input: {affixTest.Key}");
                }

                foreach (var sigilTest in _sigilTestMappings)
                {
                    var result = Process.ExtractOne(sigilTest.Key, _sigilNames, scorer: ScorerCache.Get<TokenSortScorer>());
                    var sigilId = _sigilMapNameToId[result.Value];

                    Assert.That(sigilId, Is.EqualTo(sigilTest.Value), $"Input: {sigilTest.Key}");
                }
            });
        }

        [Test]
        public void PartialTokenSortScorerTest()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappings)
                {
                    var result = Process.ExtractOne(affixTest.Key, _affixDescriptions, scorer: ScorerCache.Get<PartialTokenSortScorer>());
                    var affixId = _affixMapDescriptionToId[result.Value];

                    Assert.That(affixId, Is.EqualTo(affixTest.Value), $"Input: {affixTest.Key}");
                }

                foreach (var sigilTest in _sigilTestMappings)
                {
                    var result = Process.ExtractOne(sigilTest.Key, _sigilNames, scorer: ScorerCache.Get<PartialTokenSortScorer>());
                    var sigilId = _sigilMapNameToId[result.Value];

                    Assert.That(sigilId, Is.EqualTo(sigilTest.Value), $"Input: {sigilTest.Key}");
                }
            });
        }

        [Test]
        public void TokenAbbreviationScorerTest()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappings)
                {
                    var result = Process.ExtractOne(affixTest.Key, _affixDescriptions, scorer: ScorerCache.Get<TokenAbbreviationScorer>());
                    var affixId = _affixMapDescriptionToId[result.Value];

                    Assert.That(affixId, Is.EqualTo(affixTest.Value), $"Input: {affixTest.Key}");
                }

                foreach (var sigilTest in _sigilTestMappings)
                {
                    var result = Process.ExtractOne(sigilTest.Key, _sigilNames, scorer: ScorerCache.Get<TokenAbbreviationScorer>());
                    var sigilId = _sigilMapNameToId[result.Value];

                    Assert.That(sigilId, Is.EqualTo(sigilTest.Value), $"Input: {sigilTest.Key}");
                }
            });
        }

        [Test]
        public void PartialTokenAbbreviationScorerTest()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappings)
                {
                    var result = Process.ExtractOne(affixTest.Key, _affixDescriptions, scorer: ScorerCache.Get<PartialTokenAbbreviationScorer>());
                    var affixId = _affixMapDescriptionToId[result.Value];

                    Assert.That(affixId, Is.EqualTo(affixTest.Value), $"Input: {affixTest.Key}");
                }

                foreach (var sigilTest in _sigilTestMappings)
                {
                    var result = Process.ExtractOne(sigilTest.Key, _sigilNames, scorer: ScorerCache.Get<PartialTokenAbbreviationScorer>());
                    var sigilId = _sigilMapNameToId[result.Value];

                    Assert.That(sigilId, Is.EqualTo(sigilTest.Value), $"Input: {sigilTest.Key}");
                }
            });
        }

        [Test]
        public void WeightedRatioScorerTest()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappings)
                {
                    var result = Process.ExtractOne(affixTest.Key, _affixDescriptions, scorer: ScorerCache.Get<WeightedRatioScorer>());
                    var affixId = _affixMapDescriptionToId[result.Value];

                    Assert.That(affixId, Is.EqualTo(affixTest.Value), $"Input: {affixTest.Key}");
                }

                foreach (var sigilTest in _sigilTestMappings)
                {
                    var result = Process.ExtractOne(sigilTest.Key, _sigilNames, scorer: ScorerCache.Get<WeightedRatioScorer>());
                    var sigilId = _sigilMapNameToId[result.Value];

                    Assert.That(sigilId, Is.EqualTo(sigilTest.Value), $"Input: {sigilTest.Key}");
                }
            });
        }

        private void InitAffixData()
        {
            _affixes.Clear();
            string resourcePath = @$".\Data\Affixes.enUS.json";
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

        private void InitAffixDatazhCN()
        {
            _affixeszhCN.Clear();
            string resourcePath = @$".\Data\Affixes.zhCN.json";
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

                    _affixeszhCN = JsonSerializer.Deserialize<List<AffixInfo>>(stream, options) ?? new List<AffixInfo>();
                }
            }

            // Create affix description list for FuzzierSharp
            _affixDescriptionszhCN.Clear();
            _affixDescriptionszhCN = _affixeszhCN.Select(affix => affix.DescriptionClean).ToList();

            // Create dictionary to map affix description with affix id
            _affixMapDescriptionToIdzhCN.Clear();
            _affixMapDescriptionToIdzhCN = _affixeszhCN.ToDictionary(affix => affix.DescriptionClean, affix => affix.IdName);
        }

        private void InitSigilData()
        {
            _sigils.Clear();
            string resourcePath = @$".\Data\Sigils.enUS.json";
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

            // Create affix description list for FuzzierSharp
            _sigilNames.Clear();
            _sigilNames = _sigils.Select(affix => affix.Name).ToList();

            // Create dictionary to map affix description with affix id
            _sigilMapNameToId.Clear();
            _sigilMapNameToId = _sigils.ToDictionary(sigil => sigil.Name, sigil => sigil.IdName);
        }

        private void InitTestData()
        {
            InitTestDataenUS();
            InitTestDatazhCN();
        }

        private void InitTestDataenUS()
        {
            // Key: Input text, Value: expected affix id
            _affixTestMappings = new Dictionary<string, string>
            {
                {"+715 Thorns","Tempered_Generic_Thorns_Tier3"},
                {"+715 Thoms","Tempered_Generic_Thorns_Tier3"},
                {"18 All Stats","S04_CoreStats_All"},
                {"+10% Damage","Tempered_Damage_Generic_All_Tier3"},
                {"+19.5% Shadow Damage Over Time","Tempered_Damage_Necro_DoT_Shadow_Tier3"},
                {"+19.5% Fire Damage Over Time", "Tempered_Damage_Sorc_DoT_Burn_Tier3"},
                {"4.5% Damage Reduction [3.1 - 7.6]%","S04_DamageReduction"}
            };
            _sigilTestMappings = new Dictionary<string, string>
            {
                {"monster cold resist","DungeonAffix_Minor_Monster_LessCold"}
            };
        }

        private void InitTestDatazhCN()
        {
            // Key: Input text, Value: expected affix id
            _affixTestMappingszhCN = new Dictionary<string, string>
            {
                {"# 点全属性","S04_CoreStats_All"},
                {"+79 点智力","S04_CoreStat_Intelligence"},
                {"幸运一击：最多有 +#% 几率冻结，持续 2 秒","Tempered_Generic_LuckyHit_Freeze_Tier3"},
            };
        }
    }
}