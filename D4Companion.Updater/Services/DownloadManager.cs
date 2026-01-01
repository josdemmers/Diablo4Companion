using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Messages;
using D4Companion.Updater.Interfaces;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;
        private readonly IHttpClientHandler _httpClientHandler;

        // Start of Constructors region

        #region Constructors

        public DownloadManager(ILogger<DownloadManager> logger, HttpClientHandler httpClientHandler)
        {
            // Init services
            _httpClientHandler = httpClientHandler;
            _logger = logger;
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

        public void DeleteReleases()
        {
            try
            {
                foreach (string release in Directory.EnumerateFiles("./", "Diablo4Companion_v*.zip"))
                {
                    File.Delete(release);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
            }
        }

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
                WeakReferenceMessenger.Default.Send(new ReleaseExtractedMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        #endregion
    }
}
