using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace D4Companion.Services
{
    public class SystemPresetManager : ISystemPresetManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IHttpClientHandler _httpClientHandler;

        private List<SystemPreset> _systemPresets = new();

        // Start of Constructors region

        #region Constructors

        public SystemPresetManager(IEventAggregator eventAggregator, HttpClientHandler httpClientHandler)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<ApplicationLoadedEvent>().Subscribe(HandleApplicationLoadedEvent);

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

        public void DownloadSystemPreset(string fileName)
        {
            
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
