using D4Companion.Entities;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.IO;
using System.Text.Json;

namespace D4Companion.Services
{
    public class TradeItemManager : ITradeItemManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private List<TradeItem> _tradeItems = new List<TradeItem>();

        // Start of Constructors region

        #region Constructors

        public TradeItemManager(IEventAggregator eventAggregator, ILogger<TradeItemManager> logger, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            // Init services
            _settingsManager = settingsManager;

            // Init logger
            _logger = logger;

            LoadTradeItems();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<TradeItem> TradeItems { get => _tradeItems; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        private void LoadTradeItems()
        {
            TradeItems.Clear();

            string fileName = "Config/TradeItems.json";
            if (File.Exists(fileName))
            {
                using FileStream stream = File.OpenRead(fileName);
                _tradeItems = JsonSerializer.Deserialize<List<TradeItem>>(stream) ?? new List<TradeItem>();
            }

            SaveTradeItems();
        }

        private void SaveTradeItems()
        {
            string fileName = "Config/TradeItems.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, TradeItems, options);
        }

        public void SaveTradeItems(List<TradeItem> tradeItems)
        {
            TradeItems.Clear();
            TradeItems.AddRange(tradeItems);
            SaveTradeItems();
        }

        #endregion
    }
}
