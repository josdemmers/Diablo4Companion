using System.Threading.Tasks;

namespace D4Companion.Updater.Interfaces
{
    public interface IHttpClientHandler
    {
        Task<string> GetRequest(string uri);
        Task DownloadZip(string uri);
        Task DownloadZipSystemPreset(string uri);
    }
}