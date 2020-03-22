namespace CodeIndex.Common
{
    public interface ILog
    {
        void Info(string message);

        void Debug(string message);

        void Error(string message);

        void Trace(string message);

        void Warn(string message);
    }
}
