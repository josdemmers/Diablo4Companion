using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace D4Companion.Services
{
    public class AffixPresetManager : IAffixPresetManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private List<ItemAffix> _itemAffixes = new List<ItemAffix>();
        private List<AffixPreset> _affixPresets = new List<AffixPreset>();
        private List<ItemType> _itemTypes = new List<ItemType>();

        // Start of Constructors region

        #region Constructors

        public AffixPresetManager(IEventAggregator eventAggregator, ILogger<AffixPresetManager> logger, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<SystemPresetChangedEvent>().Subscribe(HandleSystemPresetChangedEvent);

            // Init logger
            _logger = logger;

            // Init services
            _settingsManager = settingsManager;

            // Load affix presets
            LoadAffixPresets();

            // Load item affixes
            LoadItemAffixes();

            // Load item types
            LoadItemTypes();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<AffixPreset> AffixPresets { get => _affixPresets; }
        public List<ItemAffix> ItemAffixes { get => _itemAffixes; set => _itemAffixes = value; }
        public List<ItemType> ItemTypes { get => _itemTypes; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleSystemPresetChangedEvent()
        {
            LoadAffixPresets();
            LoadItemAffixes();
            LoadItemTypes();
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

            SaveAffixPresets();

            _eventAggregator.GetEvent<AffixPresetAddedEvent>().Publish();
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

            _eventAggregator.GetEvent<AffixPresetRemovedEvent>().Publish();
        }

        private void LoadAffixPresets()
        {
            _affixPresets.Clear();

            string fileName = "Config/AffixPresets.json";
            if (File.Exists(fileName))
            {
                using FileStream stream = File.OpenRead(fileName);
                _affixPresets = JsonSerializer.Deserialize<List<AffixPreset>>(stream) ?? new List<AffixPreset>();
            }

            // Update path
            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;
            foreach (var affixPreset in _affixPresets)
            {
                foreach (var affix in affixPreset.ItemAffixes)
                {
                    affix.Path = Regex.Replace(affix.Path, "/Images/.*?/Affixes/", $"/Images/{systemPreset}/Affixes/");
                }
            }

            // Sort list
            _affixPresets.Sort((x, y) =>
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });

            SaveAffixPresets();
        }

        public void SaveAffixPresets()
        {
            string fileName = "Config/AffixPresets.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, AffixPresets, options);
        }

        private void LoadItemAffixes()
        {
            _itemAffixes.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;

            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles($"{Environment.CurrentDirectory}/Images/{systemPreset}/Affixes/");
            foreach (string filePath in fileEntries)
            {
                string fileName = Path.GetFileName(filePath);

                _itemAffixes.Add(new ItemAffix
                {
                    FileName = fileName,
                    Path = filePath
                });
            }
        }

        private void LoadItemTypes()
        {
            _itemTypes.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;

            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles($"{Environment.CurrentDirectory}/Images/{systemPreset}/Types/");
            foreach (string filePath in fileEntries)
            {
                string fileName = Path.GetFileName(filePath);
                string typeName = fileName.Split('_')[0];

                _itemTypes.Add(new ItemType
                {
                    FileName = fileName,
                    Name = typeName,
                    Path = filePath
                });
            }
        }

        #endregion


    }
}