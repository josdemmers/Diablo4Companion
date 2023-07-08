using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace D4Companion.Services
{
    public class SystemPresetManager : ISystemPresetManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IHttpClientHandler _httpClientHandler;

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

        private async void UpdateSystemPresetInfo()
        {
            /*string uri = $"https://raw.githubusercontent.com/josdemmers/NewWorldCompanion/master/NewWorldCompanion/common.props";
            string xml = await _httpClientHandler.GetRequest(uri);
            if (!string.IsNullOrWhiteSpace(xml))
            {
                var xPathDocument = new XPathDocument(new StringReader(xml));
                var xPathNavigator = xPathDocument.CreateNavigator();
                var xPathExpression = xPathNavigator.Compile("/Project/PropertyGroup/FileVersion/text()");
                var xPathNodeIterator = xPathNavigator.Select(xPathExpression);
                while (xPathNodeIterator.MoveNext())
                {
                    LatestVersion = xPathNodeIterator.Current?.ToString() ?? string.Empty;
                }
            }
            else
            {
                LatestVersion = string.Empty;
            }*/
            _eventAggregator.GetEvent<SystemPresetInfoUpdatedEvent>().Publish();
        }

        #endregion
    }
}
