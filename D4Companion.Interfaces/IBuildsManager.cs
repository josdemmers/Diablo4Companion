﻿using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Interfaces
{
    public interface IBuildsManager
    {
        List<Dictionary<string, string>> MaxrollBuilds { get; }

        void DownloadMaxrollBuild(string name);
    }
}
