using CommunityToolkit.Mvvm.Messaging;
using D4Companion.SystemPresets.Entities;
using D4Companion.SystemPresets.Interfaces;
using D4Companion.SystemPresets.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace D4Companion.SystemPresets.Services
{
    public class SystemPresetManager : ISystemPresetManager
    {
        private List<SystemPreset> _systemPresets = [];        
        private List<IconType> _iconTypes = [];

        // Start of Constructors region

        #region Constructors

        public SystemPresetManager()
        {
            // Init messages
            WeakReferenceMessenger.Default.Register<ApplicationLoadedMessage>(this, HandleApplicationLoadedMessage);

            // Init data
            InitIconTypes();
        }        

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<SystemPreset> SystemPresets { get => _systemPresets; set => _systemPresets = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleApplicationLoadedMessage(object recipient, ApplicationLoadedMessage message)
        {
            UpdateSystemPresetData();
        }        

        #endregion

        // Start of Methods region

        #region Methods

        public void AddSystemPreset(string systemPresetName)
        {
            string systemPresetsPath = @$".\SystemPresets\{systemPresetName}\";
            if (Directory.Exists(systemPresetsPath)) return;

            Directory.CreateDirectory(systemPresetsPath);

            string fileNamePath = @$".\SystemPresets\{systemPresetName}\config.json";
            using (FileStream stream = File.Create(fileNamePath))
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                JsonSerializer.Serialize(stream, new SystemPreset
                {
                    Name = systemPresetName,
                }, options);
            }

            UpdateSystemPresetData();
        }

        public List<IconType> GetItemTypes()
        {
            return _iconTypes;
        }

        private void InitIconTypes()
        {
            _iconTypes.Clear();

            _iconTypes.Add(new IconType { DisplayName = "Affix (greater)", Name = "dot-affixes_greater" });
            _iconTypes.Add(new IconType { DisplayName = "Affix (greater/master)", Name = "dot-affixes_greater_master" });
            _iconTypes.Add(new IconType { DisplayName = "Affix (masterworking)", Name = "dot-affixes_masterworking" });
            _iconTypes.Add(new IconType { DisplayName = "Affix (normal)", Name = "dot-affixes_normal" });
            _iconTypes.Add(new IconType { DisplayName = "Affix (reroll)", Name = "dot-affixes_reroll" });
        }

        public void RemoveSystemPreset(string systemPresetName)
        {
            string systemPresetsPath = @$".\SystemPresets\{systemPresetName}\";
            if (!Directory.Exists(systemPresetsPath)) return;

            Directory.Delete(systemPresetsPath, true);
            UpdateSystemPresetData();
        }

        public void Save(SystemPreset selectedSystemPreset)
        {
            selectedSystemPreset.IconTypes.Sort((x, y) =>
            {
                int result = x.DisplayName.CompareTo(y.DisplayName);
                if (result == 0)
                {
                    result = x.Count.CompareTo(y.Count);
                }
                return result;
            });

            int count = 1;
            string iconTypeName = string.Empty;
            foreach (var iconType in selectedSystemPreset.IconTypes)
            {                
                if (string.IsNullOrWhiteSpace(iconTypeName))
                {
                    // Case 1: First loop
                    iconTypeName = iconType.Name;
                    iconType.Count = count;
                }
                else if(iconTypeName.Equals(iconType.Name))
                {
                    // Case 2: Same icon type as previous loop
                    count++;
                    iconType.Count = count;
                }
                else
                {
                    // Case 3: Different icon type from previous loop
                    count = 1;
                    iconTypeName = iconType.Name;
                    iconType.Count = count;
                }
            }

            string systemPresetsPath = @$".\SystemPresets\{selectedSystemPreset.Name}\";
            string fileNamePath = @$".\SystemPresets\{selectedSystemPreset.Name}\config.json";
            using (FileStream stream = File.Create(fileNamePath))
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                JsonSerializer.Serialize(stream, selectedSystemPreset, options);
            }
        }

        public void SaveScreenshot(BitmapSource screenCapture, string systemPresetName)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(screenCapture));

            string filePath = @$".\SystemPresets\{systemPresetName}\{systemPresetName}_{DateTime.Now.Ticks}.png";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        private void updateItemTypeCounts()
        {
            foreach (var systemPreset in SystemPresets)
            {
                foreach (var iconType in systemPreset.IconTypes)
                {
                    int count = systemPreset.IconTypes.Count(preset => preset.Name.Equals(iconType.Name));
                    iconType.Count = count;
                }
            }   
        }

        private void UpdateSystemPresetData()
        {
            SystemPresets.Clear();

            string systemPresetsPath = @$".\SystemPresets\";
            if (!Directory.Exists(systemPresetsPath)) return;

            var folders = Directory.GetDirectories(systemPresetsPath);

            foreach (var folder in folders)
            {
                string configFilePath = Path.Combine(folder, "config.json");
                if (File.Exists(configFilePath))
                {
                    using FileStream stream = File.OpenRead(configFilePath);
                    var systemPreset = JsonSerializer.Deserialize<SystemPreset>(stream);
                    if (systemPreset != null)
                    {
                        SystemPresets.Add(systemPreset);
                    }
                }
            }

            WeakReferenceMessenger.Default.Send(new SystemPresetsUpdatedMessage());
        }        

        #endregion
    }
}
