using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace D4Companion.Interfaces
{
    public interface IDashboardManager
    {
        List<ThumbnailPersistent> Thumbnails { get; }

        void AddThumbnail(ThumbnailPersistent thumbnail);
        void RemoveThumbnail(ThumbnailPersistent thumbnail);
        void SaveThumbnails();
    }
}
