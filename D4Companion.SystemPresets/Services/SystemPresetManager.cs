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

            _iconTypes.Add(new IconType { DisplayName = "Splitter (top)", Name = "dot-splitter_top" });
            _iconTypes.Add(new IconType { DisplayName = "Splitter", Name = "dot-splitter" });

            _iconTypes.Add(new IconType { DisplayName = "Affix (greater)", Name = "dot-affixes_greater" });
            _iconTypes.Add(new IconType { DisplayName = "Affix (greater/master)", Name = "dot-affixes_greater_master" });
            _iconTypes.Add(new IconType { DisplayName = "Affix (masterworking)", Name = "dot-affixes_masterworking" });
            _iconTypes.Add(new IconType { DisplayName = "Affix (normal)", Name = "dot-affixes_normal" });
            _iconTypes.Add(new IconType { DisplayName = "Affix (reroll)", Name = "dot-affixes_reroll" });
            _iconTypes.Add(new IconType { DisplayName = "Affix (transfiguring)", Name = "dot-affixes_transfiguring" });            

            _iconTypes.Add(new IconType { DisplayName = "Temper (defensive)", Name = "dot-affixes_temper_defensive" });
            _iconTypes.Add(new IconType { DisplayName = "Temper (mobility)", Name = "dot-affixes_temper_mobility" });
            _iconTypes.Add(new IconType { DisplayName = "Temper (offensive)", Name = "dot-affixes_temper_offensive" });
            _iconTypes.Add(new IconType { DisplayName = "Temper (resource)", Name = "dot-affixes_temper_resource" });
            _iconTypes.Add(new IconType { DisplayName = "Temper (utility)", Name = "dot-affixes_temper_utility" });
            _iconTypes.Add(new IconType { DisplayName = "Temper (weapons)", Name = "dotdot-affixes_temper_weapons" });            

            _iconTypes.Add(new IconType { DisplayName = "Aspect (legendary)", Name = "dot-aspects_legendary" });
            _iconTypes.Add(new IconType { DisplayName = "Aspect (unique)", Name = "dot-aspects_unique" });
            _iconTypes.Add(new IconType { DisplayName = "Aspect (mythic)", Name = "dot-aspects_mythic" });            

            _iconTypes.Add(new IconType { DisplayName = "Rune (invocation)", Name = "dot-affixes_rune_invocation" });
            _iconTypes.Add(new IconType { DisplayName = "Rune (ritual)", Name = "dot-affixes_rune_ritual" });

            _iconTypes.Add(new IconType { DisplayName = "Tooltip (all)", Name = "tooltip_kb_all" });

            // Skipped for now
            // - socket / mask
            // - socket / mask (invocation)
            // - socket / mask (ritual)
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
            // Get next index
            int index = 1;
            string indexAsString = index.ToString("D3");
            var screenshots = Directory
                .GetFiles(@$".\SystemPresets\{systemPresetName}\", "*.png", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(Path.GetFileName)
                .ToList();
            if (screenshots.Count > 0)
            {
                indexAsString = screenshots[screenshots.Count - 1]!.Split("_")[2];
                index = int.Parse(indexAsString) + 1;
                indexAsString = index.ToString("D3");
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(screenCapture));

            string filePath = @$".\SystemPresets\{systemPresetName}\{systemPresetName}_{indexAsString}_{DateTime.Now.Ticks}.png";
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        public string UpdateScreenshot(BitmapSource screenCapture, string systemPresetName, string screenshot)
        {
            string indexAsString = Path.GetFileName(screenshot).Split("_")[2];

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(screenCapture));

            string filePath = @$".\SystemPresets\{systemPresetName}\{systemPresetName}_{indexAsString}_{DateTime.Now.Ticks}.png";
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }

            return filePath;
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
