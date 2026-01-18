using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Interfaces
{
    public interface IBuildsManagerD2Core
    {
        List<D2CoreBuild> D2CoreBuilds { get; }

        void CreatePresetFromD2CoreBuild(D2CoreBuild d2CoreBuild, string buildNameOriginal, string buildName);
        void DownloadD2CoreBuild(string buildIdD2Core);
        void RemoveD2CoreBuild(string buildId);
    }
}
