using D4Companion.Entities;

namespace D4Companion.Interfaces
{
    public interface IReleaseManager
    {
        List<Release> Releases { get; }
        string Repository { get; }
        bool UpdateAvailable { get; set; }

        void DownloadRelease(string url);
        void ExtractRelease(string fileName);
    }
}
