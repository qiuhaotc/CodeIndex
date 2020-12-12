namespace CodeIndex.Common
{
    public enum IndexStatus
    {
        Idle,
        Initializing,
        Initializing_ComponentInitializeFinished,
        Initialized,
        Monitoring,
        Error,
        Disposing,
        Disposed
    }
}
