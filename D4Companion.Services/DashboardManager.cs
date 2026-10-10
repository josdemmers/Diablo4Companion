using CommunityToolkit.Mvvm.Messaging;
using D4Companion.Entities;
using D4Companion.Interfaces;
using D4Companion.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace D4Companion.Services
{
    public class DashboardManager : IDashboardManager
    {
        private List<ThumbnailPersistent> _thumbnails = new List<ThumbnailPersistent>();        

        // Start of Constructors region

        #region Constructors

        public DashboardManager()
        {
            // Init data
            LoadThumbnails();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<ThumbnailPersistent> Thumbnails { get => _thumbnails; set => _thumbnails = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        public void AddThumbnail(ThumbnailPersistent thumbnail)
        {
            Thumbnails.Add(thumbnail);

            // Sort list
            Thumbnails.Sort((x, y) =>
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });

            SaveThumbnails();
        }

        private void LoadThumbnails()
        {
            Thumbnails.Clear();

            string fileName = "Config/Dashboard.json";
            if (File.Exists(fileName))
            {
                using FileStream stream = File.OpenRead(fileName);
                Thumbnails = JsonSerializer.Deserialize<List<ThumbnailPersistent>>(stream) ?? new List<ThumbnailPersistent>();
            }

            // Sort list
            Thumbnails.Sort((x, y) =>
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });

            SaveThumbnails();

        }

        public void RemoveThumbnail(ThumbnailPersistent thumbnail)
        {
            Thumbnails.Remove(thumbnail);
            SaveThumbnails();
        }

        public void SaveThumbnails()
        {
            // Sort list
            Thumbnails.Sort((x, y) =>
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });

            string fileName = "Config/Dashboard.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, Thumbnails, options);
        }        

        #endregion
    }
}
