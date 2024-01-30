using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Interfaces
{
    public interface IBuildsManager
    {
        List<MaxrollBuild> MaxrollBuilds { get; }

        void CreatePresetFromMaxrollBuild(MaxrollBuild maxrollBuild, string profile);
        void DownloadMaxrollBuild(string name);
        void RemoveMaxrollBuild(string buildId);
    }
}
