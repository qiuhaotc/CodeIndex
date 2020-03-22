using CodeIndex.Common;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace CodeIndex.Test
{
    class NLoggerTest
    {
        [Test]
        public void TestLog()
        {
            Assert.IsNull(EventInfo);

            var logger = new NLogger();
            logger.Debug("ABC");
            Assert.AreEqual(LogLevel.Debug, EventInfo.Level);
            Assert.AreEqual("ABC", EventInfo.Message);

            logger.Error("QQQQQ");
            Assert.AreEqual(LogLevel.Error, EventInfo.Level);
            Assert.AreEqual("QQQQQ", EventInfo.Message);

            logger.Info("abf");
            Assert.AreEqual(LogLevel.Info, EventInfo.Level);
            Assert.AreEqual("abf", EventInfo.Message);

            logger.Trace("dfsd");
            Assert.AreEqual(LogLevel.Trace, EventInfo.Level);
            Assert.AreEqual("dfsd", EventInfo.Message);

            logger.Warn("sdgdsfg");
            Assert.AreEqual(LogLevel.Warn, EventInfo.Level);
            Assert.AreEqual("sdgdsfg", EventInfo.Message);
        }

        [SetUp]
        protected void Setup()
        {
            var config = new LoggingConfiguration();
            config.AddTarget("Dummy", new LoggerTarget());
            config.AddRuleForAllLevels("Dummy");
            LogManager.Configuration = config;
            LogManager.Configuration.Reload();
        }

        [TearDown]
        protected void TearDown()
        {
            LogManager.Configuration = null;
            EventInfo = null;
        }

        class LoggerTarget : TargetWithLayout
        {
            protected override void Write(LogEventInfo logEvent)
            {
                base.Write(logEvent);
                EventInfo = logEvent;
            }
        }

        public static LogEventInfo EventInfo { get; set; }
    }
}
