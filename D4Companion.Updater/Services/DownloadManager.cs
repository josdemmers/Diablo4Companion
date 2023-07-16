using D4Companion.Events;
using D4Companion.Updater.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Updater.Services
{
    public class DownloadManager : IDownloadManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IHttpClientHandler _httpClientHandler;

        // Start of Constructors region

        #region Constructors

        public DownloadManager(IEventAggregator eventAggregator, ILogger<DownloadManager> logger, HttpClientHandler httpClientHandler)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

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
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion
    }
}
