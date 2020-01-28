using NLog;

namespace CodeIndex.Common
{
    public class NLogger : ILog
    {
        static Logger logger;
        public static Logger Logger => logger ?? (logger = LogManager.GetLogger(nameof(NLogger)));

        public void Info(string message)
        {
            Logger.Info(message);
        }

        public void Debug(string message)
        {
            Logger.Debug(message);
        }

        public void Error(string message)
        {
            Logger.Error(message);
        }

        public void Trace(string message)
        {
            Logger.Trace(message);
        }

        public void Warn(string message)
        {
            Logger.Warn(message);
        }
    }
}
