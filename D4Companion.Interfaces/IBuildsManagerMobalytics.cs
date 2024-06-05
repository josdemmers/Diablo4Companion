using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Interfaces
{
    public interface IBuildsManagerMobalytics
    {
        List<MobalyticsBuild> MobalyticsBuilds { get; }

        void CreatePresetFromMobalyticsBuild(MobalyticsBuildVariant mobalyticsBuild, string buildNameOriginal, string buildName);
        void DownloadMobalyticsBuild(string buildUrl);
        void RemoveMobalyticsBuild(string buildId);
    }
}
