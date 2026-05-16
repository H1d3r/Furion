using Furion.Logging;

namespace Furion.Application;

public class DatabaseLoggingWriter : IDatabaseLoggingWriter
{
    public Task WriteAsync(IReadOnlyList<LogMessage> batchLogMsgs, bool flush)
    {
        return Task.CompletedTask;
    }
}