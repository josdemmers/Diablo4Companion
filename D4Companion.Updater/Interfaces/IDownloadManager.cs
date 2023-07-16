using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Updater.Interfaces
{
    public interface IDownloadManager
    {
        void DownloadRelease(string url);
        void ExtractRelease(string fileName);
    }
}
