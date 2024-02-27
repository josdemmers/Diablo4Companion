using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Interfaces
{
    public interface IBuildsManagerD4Builds
    {
        List<D4BuildsBuild> D4BuildsBuilds { get; }

        void CreatePresetFromD4BuildsBuild(D4BuildsBuildVariant d4BuildsBuild, string buildNameOriginal, string buildName);
        void DownloadD4BuildsBuild(string buildIdD4Builds);
        void RemoveD4BuildsBuild(string buildId);
    }
}
