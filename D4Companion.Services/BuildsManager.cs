using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace D4Companion.Services
{
    public class BuildsManager : IBuildsManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly IHttpClientHandler _httpClientHandler;
        private readonly ISettingsManager _settingsManager;

        private List<Dictionary<string, string>> _maxrollBuilds = new();

        // Start of Constructors region

        #region Constructors

        public BuildsManager(IEventAggregator eventAggregator, ILogger<BuildsManager> logger, IAffixManager affixManager, IHttpClientHandler httpClientHandler, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            // Init logger
            _logger = logger;

            // Init services
            _affixManager = affixManager;
            _httpClientHandler = httpClientHandler;
            _settingsManager = settingsManager;

            // Update available presets
            Task.Factory.StartNew(() =>
            {
                UpdateAvailableMaxrollBuilds();
            });
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        //public Dictionary<string, string> MaxrollBuilds { get => _maxrollBuilds; set => _maxrollBuilds = value; }
        public List<Dictionary<string, string>> MaxrollBuilds { get => _maxrollBuilds; set => _maxrollBuilds = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        public async void DownloadMaxrollBuild(string build)
        {
            try
            {
                string uri = $"https://raw.githubusercontent.com/danparizher/maxroll-d4-scraper/main/data/translated_builds/{build}.json";
                AffixPreset? preset = null;

                string json = await _httpClientHandler.GetRequest(uri);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    preset = JsonSerializer.Deserialize<AffixPreset>(json);
                    if (preset != null) 
                    {
                        if (!string.IsNullOrEmpty(preset.Name))
                        {
                            // Note: Only allow one Maxroll build. Update if already exists.
                            _affixManager.AffixPresets.RemoveAll(p => p.Name.Equals(preset.Name));

                            // Remove duplicate affix entries
                            for (int i = preset.ItemAffixes.Count - 1; i >= 0; i--)
                            {
                                if (preset.ItemAffixes.FindAll(a => a.Id.Equals(preset.ItemAffixes[i].Id) && a.Type.Equals(preset.ItemAffixes[i].Type)).Count > 1)
                                {
                                    preset.ItemAffixes.RemoveAt(i);
                                }
                            }

                            // Add aspects to every gear slot
                            // Get all aspects from build
                            var aspectIds = preset.ItemAspects.Select(a => a.Id).ToList();
                            // Remove duplicates
                            for (int i = aspectIds.Count - 1; i >= 0; i--)
                            {
                                if (aspectIds.FindAll(a => a.Equals(aspectIds[i])).Count > 1)
                                {
                                    aspectIds.RemoveAt(i);
                                }
                            }
                            // Clear current aspect list
                            preset.ItemAspects.Clear();
                            // Add new aspects
                            foreach (var aspectId in aspectIds)
                            {
                                preset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Helm });
                                preset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Chest });
                                preset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Gloves });
                                preset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Pants });
                                preset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Boots });
                                preset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Amulet });
                                preset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Ring });
                                preset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Weapon });
                                preset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Ranged });
                                preset.ItemAspects.Add(new ItemAffix { Id = aspectId, Type = Constants.ItemTypeConstants.Offhand });
                            }

                            _affixManager.AddAffixPreset(preset);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"Invalid response. uri: {uri}");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private async void UpdateAvailableMaxrollBuilds()
        {
            try
            {
                string uri = "https://raw.githubusercontent.com/danparizher/maxroll-d4-scraper/main/data/builds.json";

                string json = await _httpClientHandler.GetRequest(uri);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    MaxrollBuilds.Clear();
                    MaxrollBuilds = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json) ?? new List<Dictionary<string, string>>();
                }
                else
                {
                    _logger.LogWarning($"Invalid response. uri: {uri}");
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
