using System.Collections.Generic;
using Vortice.DXGI;

namespace D4Companion.SystemPresets
{
    public static class MonitorCaptureFactory
    {
        public static List<MonitorDuplicator> CreateAllDuplicators()
        {
            var duplicators = new List<MonitorDuplicator>();

            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();

            uint adapterIndex = 0;
            while (factory.EnumAdapters1(adapterIndex, out IDXGIAdapter1 adapter).Success)
            {
                uint outputIndex = 0;
                while (adapter.EnumOutputs(outputIndex, out IDXGIOutput output).Success)
                {
                    var desc = output.Description;

                    if (desc.AttachedToDesktop)
                    {
                        duplicators.Add(new MonitorDuplicator(factory, adapterIndex, outputIndex));
                    }

                    output.Dispose();
                    outputIndex++;
                }

                adapter.Dispose();
                adapterIndex++;
            }

            return duplicators;
        }
    }
}