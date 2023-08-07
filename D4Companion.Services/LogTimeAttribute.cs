using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Reflection;

[module: D4Companion.Services.LogTime]

namespace D4Companion.Services
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly | AttributeTargets.Module)]
    public class LogTimeAttribute : Attribute
    {
        private Stopwatch? _stopwatch;

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
            _stopwatch = Stopwatch.StartNew();
        }

        public void OnExit()
        {
            if (_stopwatch != null)
            {
                _stopwatch.Stop();
                _logger!.LogDebug($"{_method!.Name}: Elapsed time: {_stopwatch!.ElapsedMilliseconds}");
            }
        }

        public void OnException(Exception exception)
        {
            throw exception;
        }
    }
}
