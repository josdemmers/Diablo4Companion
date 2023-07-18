using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Emgu.CV.Structure;
using Emgu.CV;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace D4Companion.Services
{
    public class AffixPresetManager : IAffixPresetManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private List<ItemAffix> _itemAffixes = new List<ItemAffix>();
        private List<ItemAspect> _itemAspects = new List<ItemAspect>();
        private List<AffixPreset> _affixPresets = new List<AffixPreset>();
        private List<ItemType> _itemTypes = new List<ItemType>();
        private List<ItemType> _itemTypesLite = new List<ItemType>();

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

            // Load item aspects
            LoadItemAspects();

            // Load item types
            LoadItemTypes();
            LoadItemTypesLite();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<AffixPreset> AffixPresets { get => _affixPresets; }
        public List<ItemAffix> ItemAffixes { get => _itemAffixes; set => _itemAffixes = value; }
        public List<ItemAspect> ItemAspects { get => _itemAspects; set => _itemAspects = value; }
        public List<ItemType> ItemTypes { get => _itemTypes; }
        public List<ItemType> ItemTypesLite { get => _itemTypesLite; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleSystemPresetChangedEvent()
        {
            LoadAffixPresets();
            LoadItemAffixes();
            LoadItemAspects();
            LoadItemTypes();
            LoadItemTypesLite();
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
            string directory = $"Images\\{systemPreset}\\";
            if (!Directory.Exists(directory))
            {
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"System preset not found at \"{directory}\". Go to settings to select one."
                });
                return;
            }

            // Process the list of files found in the directory.
            var fileEntries = Directory.EnumerateFiles($"{Environment.CurrentDirectory}/Images/{systemPreset}/Affixes/").Where(affix => affix.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
            foreach (string filePath in fileEntries)
            {
                string fileName = Path.GetFileName(filePath).ToLower();

                _itemAffixes.Add(new ItemAffix
                {
                    FileName = fileName
                });
            }
        }

        private void LoadItemAspects()
        {
            _itemAspects.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;
            string directory = $"Images\\{systemPreset}\\";
            if (!Directory.Exists(directory))
            {
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                {
                    Message = $"System preset not found at \"{directory}\". Go to settings to select one."
                });
                return;
            }

            // Process the list of files found in the directory.
            var fileEntries = Directory.EnumerateFiles($"{Environment.CurrentDirectory}/Images/{systemPreset}/Aspects/").Where(aspect => aspect.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
            foreach (string filePath in fileEntries)
            {
                string fileName = Path.GetFileName(filePath).ToLower();

                _itemAspects.Add(new ItemAspect
                {
                    FileName = fileName
                });
            }
        }

        private void LoadItemTypes()
        {
            _itemTypes.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;

            var directory = $"Images\\{systemPreset}\\Types\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(itemType => itemType.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !itemType.ToLower().Contains("weapon_all"));
                foreach (string filePath in fileEntries)
                {
                    string fileName = Path.GetFileName(filePath).ToLower();
                    string typeName = fileName.Split('_')[0].ToLower();

                    _itemTypes.Add(new ItemType
                    {
                        FileName = fileName,
                        Name = typeName
                    });
                }
            }
        }

        private void LoadItemTypesLite()
        {
            _itemTypesLite.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;

            var directory = $"Images\\{systemPreset}\\Types\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(itemType => itemType.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                (itemType.ToLower().Contains("weapon_all") || (!itemType.ToLower().Contains("weapon_") && !itemType.ToLower().Contains("ranged_") && !itemType.ToLower().Contains("offhand_focus"))));
                foreach (string filePath in fileEntries)
                {
                    string fileName = Path.GetFileName(filePath).ToLower();
                    string typeName = fileName.Split('_')[0].ToLower();

                    _itemTypesLite.Add(new ItemType
                    {
                        FileName = fileName,
                        Name = typeName
                    });
                }
            }
        }

        #endregion

    }
}