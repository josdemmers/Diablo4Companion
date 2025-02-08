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
    public class ReleaseManager : IReleaseManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IHttpClientHandler _httpClientHandler;
        private readonly ISettingsManager _settingsManager;

        private List<Release> _releases = new List<Release>();
        private bool _updateAvailable = false;

        // Start of Constructors region

        #region Constructors

        public ReleaseManager(IEventAggregator eventAggregator, ILogger<ReleaseManager> logger, IHttpClientHandler httpClientHandler, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            // Init logger
            _logger = logger;

            // Init services
            _httpClientHandler = httpClientHandler;
            _settingsManager = settingsManager;

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
        public bool UpdateAvailable { get => _updateAvailable; set => _updateAvailable = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        public async void DownloadRelease(string url)
        {
            _logger.LogInformation($"Downloading: {url}");

            await _httpClientHandler.DownloadZip(url);
        }

        public void ExtractRelease(string fileName)
        {
            try
            {
                _logger.LogInformation($"Extracting: {fileName}");

                // Change the currently running executable so it can be overwritten.
                var app = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "D4Companion.Updater.exe";
                app = Path.GetFileName(app);
                var bak = $"{app}.bak";
                if (File.Exists(bak)) File.Delete(bak);
                File.Move(app, bak);
                File.Copy(bak, app);

                ZipFile.ExtractToDirectory(fileName, "./", true);
                _eventAggregator.GetEvent<ReleaseExtractedEvent>().Publish();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private async void UpdateAvailableReleases()
        {
            try
            {
                if (_settingsManager.Settings.CheckForUpdates) 
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
                else
                {
                    _logger.LogInformation($"Check for updates disabled by user.");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion
    }
}
