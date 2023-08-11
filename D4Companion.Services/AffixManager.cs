using D4Companion.Entities;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace D4Companion.Services
{
    public class AffixManager : IAffixManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;

        private List<AffixInfo> _affixes = new List<AffixInfo>();

        // Start of Constructors region

        #region Constructors

        public AffixManager(IEventAggregator eventAggregator, ILogger<AffixManager> logger)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;

            // Init logger
            _logger = logger;

            // Init store data
            InitAffixData();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<AffixInfo> Affixes { get => _affixes; set => _affixes = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        private void InitAffixData()
        {
            _affixes.Clear();
            string resourcePath = @".\Data\Affixes.json";
            using (FileStream? stream = File.OpenRead(resourcePath))
            {
                if (stream != null)
                {
                    // create the options
                    var options = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    // register the converter
                    options.Converters.Add(new BoolConverter());
                    options.Converters.Add(new IntConverter());

                    _affixes = JsonSerializer.Deserialize<List<AffixInfo>>(stream, options) ?? new List<AffixInfo>();
                }
            }
        }

        #endregion
    }
}
