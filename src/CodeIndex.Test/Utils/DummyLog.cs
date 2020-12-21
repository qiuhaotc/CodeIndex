using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CodeIndex.Test
{
    [ExcludeFromCodeCoverage]
    public class DummyLog : ILogger
    {
        readonly StringBuilder logs = new ();

        public string ThrowExceptionWhenLogContains { get; set; }

        public string LogsContent => logs.ToString();

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public void ClearLog()
        {
            logs.Clear();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Log(formatter.Invoke(state, exception));
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

    public class DummyLog<T> : DummyLog, ILogger<T>
    {
    }
}
