namespace D4Companion.Interfaces
{
    public interface IHttpClientHandler
    {
        Task<string> GetRequest(string uri);
        Task DownloadZip(string uri);
        Task DownloadZipSystemPreset(string uri);
    }
}