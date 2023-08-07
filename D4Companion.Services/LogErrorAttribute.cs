using Microsoft.Extensions.Logging;

using System.Reflection;

[module: D4Companion.Services.LogError]

namespace D4Companion.Services
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly | AttributeTargets.Module)]
    public class LogErrorAttribute : Attribute
    {

        private MethodBase? _method;

        private ILogger? _logger;

        public void Init(object instance, MethodBase method, object[] args)
        {
            _logger = (ILogger)method.DeclaringType!
                .GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(instance)!;
            _method = method;
        }

        public void OnEntry()
        {
        }

        public void OnExit()
        {
        }

        public void OnException(Exception exception)
        {
            _logger!.LogError(exception, _method!.Name);
        }
    }
}
