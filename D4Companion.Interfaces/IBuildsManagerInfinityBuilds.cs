using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Interfaces
{
    public interface IBuildsManagerInfinityBuilds
    {
        List<InfinityBuildsBuild> InfinityBuildsBuilds { get; }

        void CreatePresetFromInfinityBuildsBuild(InfinityBuildsBuildVariant infinityBuildsBuild, string buildNameOriginal, string buildName);
        void DownloadInfinityBuildsBuild(string buildUrlInfinityBuilds);
        void RemoveInfinityBuildsBuild(string buildId);
    }
}
