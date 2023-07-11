using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Text.Json;

namespace D4Companion.Services
{
    public class ReleaseManager : IReleaseManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IHttpClientHandler _httpClientHandler;

        private List<Release> _releases = new List<Release>();

        // Start of Constructors region

        #region Constructors

        public ReleaseManager(IEventAggregator eventAggregator, ILogger<ReleaseManager> logger, HttpClientHandler httpClientHandler)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            // Init logger
            _logger = logger;

            // Init services
            _httpClientHandler = httpClientHandler;

            // Update release info
            Task.Factory.StartNew(() =>
            {
                UpdateAvailableReleases();
            });
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<Release> Releases { get => _releases; set => _releases = value; }
        public string Repository { get; } = "https://api.github.com/repos/josdemmers/diablo4Companion/releases";

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        private async void UpdateAvailableReleases()
        {
            _logger.LogInformation($"Updating release info from: {Repository}");

            string json = await _httpClientHandler.GetRequest(Repository);
            if (!string.IsNullOrWhiteSpace(json))
            {
                Releases.Clear();
                Releases = JsonSerializer.Deserialize<List<Release>>(json) ?? new List<Release>();

            }
            else
            {
                _logger.LogWarning($"Invalid response. uri: {Repository}");
            }
            _eventAggregator.GetEvent<ReleaseInfoUpdatedEvent>().Publish();
        }

        #endregion
    }
}
