using D4Companion.Events;
using D4Companion.Updater.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace D4Companion.Updater.Services
{
    public class HttpClientHandler : IHttpClientHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;

        // HttpClient is intended to be instantiated once per application, rather than per-use.
        private HttpClient _client;

        // Start of Constructor region

        #region Constructor

        public HttpClientHandler(IEventAggregator eventAggregator, ILogger<HttpClientHandler> logger)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            // Init logger
            _logger = logger;

            // Init client
            InitClient();
        }

        #endregion

        // Start of Properties region

        #region Properties

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Methods region

        #region Methods

        private void InitClient()
        {
            var progressMessageHandler = new ProgressMessageHandler(new System.Net.Http.HttpClientHandler());
            progressMessageHandler.HttpSendProgress += (sender, e) =>
            {
                _eventAggregator.GetEvent<UploadProgressUpdatedEvent>().Publish(new HttpProgress
                {
                    Bytes = e.BytesTransferred,
                    Progress = e.ProgressPercentage
                });
            };

            progressMessageHandler.HttpReceiveProgress += (sender, e) =>
            {
                _eventAggregator.GetEvent<DownloadProgressUpdatedEvent>().Publish(new HttpProgress
                {
                    Bytes = e.BytesTransferred,
                    Progress = e.ProgressPercentage
                });
            };
            _client = new HttpClient(progressMessageHandler);

            // Config client
            _client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };
            _client.DefaultRequestHeaders.Add("User-Agent", "D4Companion");
        }

        public async Task<string> GetRequest(string uri)
        {
            HttpResponseMessage? response = null;
            string responseAsString = string.Empty;
            try
            {
                response = await _client.GetAsync(uri);
                responseAsString = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
                return responseAsString;
            }
            catch (Exception ex)
            {
                if (response != null)
                {
                    _logger.LogError(ex, $"GetRequest({uri}). Response: {responseAsString}");
                }
                else
                {
                    _logger.LogError(ex, $"GetRequest({uri}).");
                }

                return responseAsString;
            }
        }

        public async Task DownloadZip(string uri)
        {
            Stream? stream = null;

            try
            {
                var fileName = Path.GetFileName(uri);
                if (string.IsNullOrWhiteSpace(fileName)) return;

                stream = await _client.GetStreamAsync(uri);
                using (var fileStream = File.Create(fileName))
                {
                    stream.CopyTo(fileStream);
                }

                _eventAggregator.GetEvent<DownloadCompletedEvent>().Publish(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetRequest({uri}).");
            }
        }

        public async Task DownloadZipSystemPreset(string uri)
        {
            Stream? stream = null;

            try
            {
                var fileName = Path.GetFileName(uri);
                if (string.IsNullOrWhiteSpace(fileName)) return;

                stream = await _client.GetStreamAsync(uri);
                using (var fileStream = File.Create($".\\Images\\{fileName}"))
                {
                    stream.CopyTo(fileStream);
                }

                _eventAggregator.GetEvent<DownloadSystemPresetCompletedEvent>().Publish(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetRequest({uri}).");
            }
        }

        #endregion
    }
}