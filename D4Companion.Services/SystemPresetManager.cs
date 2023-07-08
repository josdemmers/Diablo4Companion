using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
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

        private List<SystemPreset> _systemPresets = new();

        // Start of Constructors region

        #region Constructors

        public SystemPresetManager(IEventAggregator eventAggregator, ILogger<SystemPresetManager> logger, HttpClientHandler httpClientHandler)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);

            // Init logger
            _logger = logger;

            // Init services
            _httpClientHandler = httpClientHandler;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<SystemPreset> SystemPresets { get => _systemPresets; set => _systemPresets = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleApplicationLoadedEvent()
        {
            UpdateSystemPresetInfo();
        }

        #endregion

        // Start of Methods region

        #region Methods

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

        private async void UpdateSystemPresetInfo()
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

        #endregion
    }
}
