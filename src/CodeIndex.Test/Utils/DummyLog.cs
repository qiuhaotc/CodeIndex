using System.Text;
using CodeIndex.Common;

namespace CodeIndex.Test
{
    class DummyLog : ILog
    {
        readonly StringBuilder logs = new StringBuilder();

        public string LogsContent => logs.ToString();

        public void ClearLog()
        {
            logs.Clear();
        }

        public void Debug(string message)
        {
            logs.AppendLine(message);
        }

        public void Error(string message)
        {
             logs.AppendLine(message);
        }

        public void Info(string message)
        {
             logs.AppendLine(message);
        }

        public void Trace(string message)
        {
             logs.AppendLine(message);
        }

        public void Warn(string message)
        {
             logs.AppendLine(message);
        }
    }
}
