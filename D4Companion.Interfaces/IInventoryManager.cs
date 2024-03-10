using D4Companion.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Interfaces
{
    public interface IInventoryManager
    {
        Inventory Inventory { get; }

        void DecreaseAspectCount(string aspectId);
        int GetAspectCount(string aspectId);
        void IncreaseAspectCount(string aspectId);
        void ResetAspectCount(string aspectId);
    }
}
