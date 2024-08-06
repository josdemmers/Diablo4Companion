using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

namespace D4Companion.Services
{
    public class SystemPresetManager : ISystemPresetManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IHttpClientHandler _httpClientHandler;
        private readonly ISettingsManager _settingsManager;

        private List<string> _controllerConfig = new();
        private List<string> _controllerImages = new();
        private List<SystemPreset> _systemPresets = new();
        
        // Start of Constructors region

        #region Constructors

        public SystemPresetManager(IEventAggregator eventAggregator, ILogger<SystemPresetManager> logger, IHttpClientHandler httpClientHandler, ISettingsManager settingsManager)
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
            LoadControllerConfig();
            LoadControllerImages();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<string> ControllerConfig { get => _controllerConfig; set => _controllerConfig = value; }
        public List<string> ControllerImages { get => _controllerImages; set => _controllerImages = value; }
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
            LoadControllerConfig();
            LoadControllerImages();
        }

        #endregion

        // Start of Methods region

        #region Methods

        public void AddController(string fileName)
        {
            if (!ControllerConfig.Any(c => c.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                ControllerConfig.Add(fileName);
            }

            SaveControllerConfig();
        }

        public void RemoveController(string fileName)
        {
            ControllerConfig.Remove(fileName);
            SaveControllerConfig();
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

        public bool IsControllerActive(string fileName)
        {
            return ControllerConfig.Any(c => c.Equals(fileName));
        }

        private void LoadControllerConfig()
        {
            ControllerConfig.Clear();

            string fileName = $"Config/Controllers.json";
            if (File.Exists(fileName))
            {
                using FileStream stream = File.OpenRead(fileName);
                ControllerConfig = JsonSerializer.Deserialize<List<string>>(stream) ?? new List<string>();
            }

            SaveControllerConfig();
        }

        private void SaveControllerConfig()
        {
            string fileName = $"Config/Controllers.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, ControllerConfig, options);
        }

        public void LoadControllerImages()
        {
            _controllerImages.Clear();

            string systemPreset = _settingsManager.Settings.SelectedSystemPreset;

            var directory = $"Images\\{systemPreset}\\Tooltips\\";
            if (Directory.Exists(directory))
            {
                var fileEntries = Directory.EnumerateFiles(directory).Where(filePath => filePath.Contains("tooltip_gc_", StringComparison.OrdinalIgnoreCase));

                foreach (string filePath in fileEntries)
                {
                    string fileName = Path.GetFileName(filePath).ToLower();
                    _controllerImages.Add(fileName);
                }
            }
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
