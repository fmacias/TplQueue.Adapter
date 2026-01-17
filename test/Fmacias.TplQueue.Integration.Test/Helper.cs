using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace Fmaciasruano.TplQueue.Core.Integration.Test
{
    internal class Helper
    {
        public static ILogger<T> GetLogger<T>()
        {
            var workDir = TestContext.CurrentContext.WorkDirectory;
            var configPath = Path.Combine(workDir, "NLog.config");

            LogManager.Setup().LoadConfigurationFromFile(configPath);

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddNLog();
            });
            return loggerFactory.CreateLogger<T>();
        }
    }
}
