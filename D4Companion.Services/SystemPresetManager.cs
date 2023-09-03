using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace D4Companion.Services
{
    public class SystemPresetManager : ISystemPresetManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IHttpClientHandler _httpClientHandler;
        private readonly ISettingsManager _settingsManager;

        private List<string> _affixImages = new();
        private List<AffixMapping> _affixMappings = new();
        private List<ItemType> _itemTypes = new();
        private List<SystemPreset> _systemPresets = new();
        
        // Start of Constructors region

        #region Constructors

        public SystemPresetManager(IEventAggregator eventAggregator, ILogger<SystemPresetManager> logger, HttpClientHandler httpClientHandler, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);
            _eventAggregator.GetEvent<SystemPresetChangedEvent>().Subscribe(HandleSystemPresetChangedEvent);

            // Init logger
            _logger = logger;

            // Init services
            _httpClientHandler = httpClientHandler;
            _settingsManager = settingsManager;

            // Load data
            LoadAffixMappings();
            LoadAffixImages();
            LoadItemTypes();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<string> AffixImages { get => _affixImages; set => _affixImages = value; }
        public List<AffixMapping> AffixMappings { get => _affixMappings; set => _affixMappings = value; }
        public List<SystemPreset> SystemPresets { get => _systemPresets; set => _systemPresets = value; }
        

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleApplicationLoadedEvent()
        {
            UpdateSystemPresetInfo();
        }

        private void HandleSystemPresetChangedEvent()
        {
            LoadAffixMappings();
            LoadAffixImages();
            LoadItemTypes();
        }

        #endregion

        // Start of Methods region

        #region Methods

        public void AddMapping(string idName, string folder, string fileName)
        {
            var mapping = AffixMappings.FirstOrDefault(mapping => mapping.IdName.Equals(idName));
            if (mapping == null)
            {
                AffixMappings.Add(new AffixMapping
                {
                    IdName = idName,
                    Folder = folder,
                    Images = new List<string> { fileName }
                });
            }
            else
            {
                var fileNameImage = mapping.Images.FirstOrDefault(image => image.Equals(fileName));
                if (string.IsNullOrWhiteSpace(fileNameImage))
                {
                    mapping.Images.Add(fileName);
                }
            }

            SaveAffixMappings();
        }

        public void RemoveMapping(string idName, string folder, string fileName)
        {
            var mapping = AffixMappings.FirstOrDefault(mapping => mapping.IdName.Equals(idName));
            if (mapping == null) return;

            var fileNameImage = mapping.Images.FirstOrDefault(image => image.Equals(fileName));
            if (string.IsNullOrWhiteSpace(fileNameImage)) return;

            mapping.Images.Remove(fileNameImage);

            if (mapping.Images.Count == 0)
            {
                AffixMappings.Remove(mapping);
            }

            SaveAffixMappings();
        }

        public async void DownloadSystemPreset(string fileName)
        {
            string uri = $"https://github.com/josdemmers/Diablo4Companion/raw/master/downloads/systempresets/{fileName}";

            await _httpClientHandler.DownloadZipSystemPreset(uri);
        }

        public void ExtractSystemPreset(string fileName)
        {
            try
            {
                ZipFile.ExtractToDirectory($".\\Images\\{fileName}", ".\\Images", true);
                _eventAggregator.GetEvent<SystemPresetExtractedEvent>().Publish();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        public int GetImageUsageCount(string folder, string fileName)
        {
            int count = 0;

            foreach (var mapping in AffixMappings) 
            {
                if (mapping.Images.Count == 0) { continue; }
                if (mapping.Folder.Equals(folder))
                {
                    if (mapping.Images.Contains(fileName))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public bool IsItemTypeImageFound(string itemType)
        {
            if (string.IsNullOrEmpty(itemType)) return _itemTypes.Any();
            return _itemTypes.Any(type => type.Name.Equals(itemType, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadAffixMappings()
        {
            AffixMappings.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;

            string fileName = $"Images/{systemPreset}/Mappings.json";
            if (File.Exists(fileName))
            {
                using FileStream stream = File.OpenRead(fileName);
                AffixMappings = JsonSerializer.Deserialize<List<AffixMapping>>(stream) ?? new List<AffixMapping>();
            }

            SaveAffixMappings();

            // TODO: Might need this event to update mapping GUI
            //_eventAggregator.GetEvent<SystemPresetAffixMappingsLoadedEvent>().Publish();
        }

        private void SaveAffixMappings()
        {
            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;
            string fileName = $"Images/{systemPreset}/Mappings.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, AffixMappings, options);
        }

        private void LoadAffixImages()
        {
            _affixImages.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;

            var directory = $"Images\\{systemPreset}\\Affixes\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(filePath => filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

                foreach (string filePath in fileEntries)
                {
                    string fileName = Path.GetFileName(filePath).ToLower();
                    _affixImages.Add(fileName);
                }
            }

            // TODO: Might need this event to update mapping GUI
            //_eventAggregator.GetEvent<SystemPresetAffixImagesLoadedEvent>().Publish();
        }

        private void LoadItemTypes()
        {
            _itemTypes.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;

            var directory = $"Images\\{systemPreset}\\Types\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(itemType => itemType.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
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

            _eventAggregator.GetEvent<SystemPresetItemTypesLoadedEvent>().Publish();
        }

        private async void UpdateSystemPresetInfo()
        {
            try
            {
                string uri = $"https://raw.githubusercontent.com/josdemmers/Diablo4Companion/master/downloads/systempresets/systempresets.json";
                string json = await _httpClientHandler.GetRequest(uri);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    _systemPresets.Clear();
                    _systemPresets = JsonSerializer.Deserialize<List<SystemPreset>>(json) ?? new List<SystemPreset>();
                }
                else
                {
                    _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventParams
                    {
                        Message = $"Not able to download the latest system presets."
                    });
                }
                _eventAggregator.GetEvent<SystemPresetInfoUpdatedEvent>().Publish();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion
    }
}
