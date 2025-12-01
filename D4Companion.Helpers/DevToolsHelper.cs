using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;

namespace D4Companion.Helpers
{
    public static class DevToolsHelper
    {
        public static Type GetTypeFromNetworkNamespaceByName(DevToolsSession session, string typeByName)
        {
            var domains = GetLatestDomains(session);

            // Use reflection to access Network domain
            var networkProp = domains.GetType().GetProperty("Network");
            var networkObj = networkProp?.GetValue(domains);

            //if (networkProp == null) throw new InvalidOperationException("Network property not found in DevTools domains.");
            //if (networkObj == null) throw new InvalidOperationException("Network domain object is null.");

            Type? reflectionType;
            try
            {
                reflectionType = networkObj.GetType().Assembly
                .GetTypes()
                .First(t => t.Name == typeByName && t.Namespace != null && t.Namespace.Contains("Network"));
            }
            catch (ReflectionTypeLoadException ex)
            {
                reflectionType = ex.Types.First(t => t.Name == typeByName && t.Namespace != null && t.Namespace.Contains("Network"));
            }
            return reflectionType;
        }

        public static object GetLatestDomains(DevToolsSession session)
        {
            // Find all DevToolsSessionDomains types in the Selenium assembly
            Type[] domainTypes;
            try
            {
                var assembly = typeof(DevToolsSession).Assembly;
                domainTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                domainTypes = ex.Types.Where(t => t != null).ToArray();
            }

            domainTypes = domainTypes
                .Where(t => t.Name == "DevToolsSessionDomains" &&
                            t.Namespace != null &&
                            t.Namespace.StartsWith("OpenQA.Selenium.DevTools.V")).ToArray();

            if (!domainTypes.Any())
            {
                throw new InvalidOperationException("No DevTools domains found in Selenium assembly.");
            }

            // Pick the highest version (largest Vxxx)
            var latestType = domainTypes
                .OrderByDescending(t =>
                {
                    var ns = t.Namespace;
                    var versionStr = ns.Split('.').Last().TrimStart('V');
                    return int.TryParse(versionStr, out var v) ? v : 0;
                })
                .First();

            // Call GetVersionSpecificDomains<T>() dynamically
            var method = typeof(DevToolsSession).GetMethods()
                .First(m => m.Name == "GetVersionSpecificDomains" && m.IsGenericMethod);

            var generic = method.MakeGenericMethod(latestType);
            return generic.Invoke(session, null);
        }
    }
}