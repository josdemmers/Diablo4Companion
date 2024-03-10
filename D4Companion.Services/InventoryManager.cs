using D4Companion.Entities;
using D4Companion.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace D4Companion.Services
{
    public class InventoryManager : IInventoryManager
    {
        private Inventory _inventory = new();

        // Start of Constructors region

        #region Constructors

        public InventoryManager()
        {
            // Load data
            LoadInventory();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public Inventory Inventory { get => _inventory; set => _inventory = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        public void DecreaseAspectCount(string aspectId)
        {
            Inventory.Aspects.TryAdd(aspectId, 0);

            if (Inventory.Aspects.TryGetValue(aspectId, out var aspectCount))
            {
                Inventory.Aspects[aspectId] = Math.Max(0, --aspectCount);
            }

            SaveInventory();
        }

        public int GetAspectCount(string aspectId)
        {
            if (Inventory.Aspects.TryGetValue(aspectId, out var aspectCount))
            {
                return aspectCount;
            }

            return 0;
        }

        public void IncreaseAspectCount(string aspectId)
        {
            Inventory.Aspects.TryAdd(aspectId, 0);

            if (Inventory.Aspects.TryGetValue(aspectId, out var aspectCount))
            {
                Inventory.Aspects[aspectId] = Math.Min(9, ++aspectCount);
            }

            SaveInventory();
        }

        private void LoadInventory()
        {
            Inventory.Aspects.Clear();

            string fileName = $"Config/Inventory.json";
            if (File.Exists(fileName))
            {
                using FileStream stream = File.OpenRead(fileName);
                Inventory = JsonSerializer.Deserialize<Inventory>(stream) ?? new Inventory();
            }

            SaveInventory();
        }

        public void ResetAspectCount(string aspectId)
        {
            Inventory.Aspects.TryAdd(aspectId, 0);

            if (Inventory.Aspects.ContainsKey(aspectId))
            {
                Inventory.Aspects[aspectId] = 0;
            }

            SaveInventory();
        }

        private void SaveInventory()
        {
            string fileName = $"Config/Inventory.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, Inventory, options);
        }

        #endregion
    }
}
