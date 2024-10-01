using D4Companion.Entities;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using D4Companion.Services;
using FuzzierSharp.SimilarityRatio.Scorer.StrategySensitive;
using FuzzierSharp.SimilarityRatio;
using System.Text.Json;
using FuzzierSharp;
using FuzzierSharp.SimilarityRatio.Scorer.Composite;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using System.Globalization;

namespace D4Companion.Tests
{
    public class RegexTests
    {
        // enUS
        private Dictionary<string, double> _affixTestMappings = new Dictionary<string, double>();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Init data
            InitTestData();
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void RegexAffixValuesTest()
        {
            Assert.Multiple(() =>
            {
                foreach (var affixTest in _affixTestMappings)
                {
                    string textClean = String.Concat(affixTest.Key.Where(c =>
                                (c != '[') &&
                                (c != ']') &&
                                (c != '(') &&
                                (c != ')') &&
                                (c != '+') &&
                                (c != '-') &&
                                (c != '%'))).Trim();

                    string textValue = Regex.Match(textClean, @"\d+\.\d+|\d+\,\d+|\d+").Value;
                    textValue = textValue.IndexOf(".") == textValue.Length - 2 ? textValue : textValue.Replace(".", string.Empty);
                    textValue = textValue.IndexOf(",") == textValue.Length - 2 ? textValue : textValue.Replace(",", string.Empty);

                    textValue = textValue.Replace(',', '.');
                    double affixValue = double.Parse(textValue, CultureInfo.InvariantCulture);

                    Assert.That(affixValue, Is.EqualTo(affixTest.Value), $"Original Input: {affixTest.Key} {Environment.NewLine}Input: {textClean}");
                }
            });
        }

        private void InitTestData()
        {
            InitTestDataenUS();
        }

        private void InitTestDataenUS()
        {
            // Key: Input text, Value: expected value
            _affixTestMappings = new Dictionary<string, double>
            {
                {"+32.0% Vulnerable Damage [32.0]%",32.0},
                {"+32,0% Vulnerable Damage [32,0]%",32.0},
                {"+174 Dexterity +[152 - 180]",174},
                {"+1,394 Maximum Life [1,276 - 1,746]",1394},
                {"+1.394 Maximum Life [1.276 - 1.746]",1394},
                {"+96.0% Critical Strike Damage [80.0 - 100.0]%",96.0},
                {"+96,0% Critical Strike Damage [80,0 - 100,0]%",96.0},
                {"+26.0% Chance for Barrage Projectiles to Cast Twice [26.0 - 35.0]% (Rogue Only)",26.0}
            };
        }
    }
}