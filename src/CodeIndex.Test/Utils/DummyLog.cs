using System;
using System.Text;
using CodeIndex.Common;

namespace CodeIndex.Test
{
    class DummyLog : ILog
    {
        readonly StringBuilder logs = new StringBuilder();

        public string ThrowExceptionWhenLogContains { get; set; }

        public string LogsContent => logs.ToString();

        public void ClearLog()
        {
            logs.Clear();
        }

        public void Debug(string message)
        {
            Log(message);
        }

        public void Error(string message)
        {
            Log(message);
        }

        public void Info(string message)
        {
            Log(message);
        }

        public void Trace(string message)
        {
            Log(message);
        }

        public void Warn(string message)
        {
            Log(message);
        }

        void Log(string message)
        {
            if (!string.IsNullOrEmpty(ThrowExceptionWhenLogContains) && message.Contains(ThrowExceptionWhenLogContains))
            {
                throw new Exception("Dummy Error");
            }

            logs.AppendLine(message);
        }
    }
}
