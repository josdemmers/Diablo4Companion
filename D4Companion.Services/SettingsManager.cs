using D4Companion.Entities;
using D4Companion.Interfaces;
using Prism.Events;
using System.IO;
using System.Text.Json;

namespace D4Companion.Services
{
    public class SettingsManager : ISettingsManager
    {
        private readonly IEventAggregator _eventAggregator;

        private SettingsD4 _settings = new SettingsD4();

        // Start of Constructors region

        #region Constructors

        public SettingsManager(IEventAggregator eventAggregator)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            LoadSettings();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public SettingsD4 Settings { get => _settings; set => _settings = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        public void LoadSettings()
        {
            string fileName = "Config/Settings.json";
            if (File.Exists(fileName))
            {
                using FileStream stream = File.OpenRead(fileName);
                _settings = JsonSerializer.Deserialize<SettingsD4>(stream) ?? new SettingsD4();
            }

            SaveSettings();
        }

        public void SaveSettings()
        {
            string fileName = "Config/Settings.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, _settings, options);
        }

        #endregion
    }
}
