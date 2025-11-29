using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.Logger;

public class Logger : ILogger
{
    private event EventHandler<LogEntry>? LogProcessors;
    private readonly HashSet<EventHandler<LogEntry>> _subscribedHandlers = new();

    public void Subscribe(EventHandler<LogEntry> handler)
    {
        if (_subscribedHandlers.Add(handler))
        {
            LogProcessors += handler;
        }
    }

    public void Debug(string log)
    {
        this._Log(LogLevel.Debug, log);
    }

    public void Info(string log)
    {
        this._Log(LogLevel.Info, log);
    }

    public void Error(string log)
    {
        this._Log(LogLevel.Error, log);
    }

    public void Warning(string log)
    {
        this._Log(LogLevel.Warning, log);
    }

    public void Critical(string log)
    {
        this._Log(LogLevel.Critical, log);
    }
    public void Status(string log)
    {
        this._Log(LogLevel.Status, log);
    }

    private void _Log(LogLevel level, string log)
    {
        if (LogProcessors != null)
        {
            LogProcessors(this, new LogEntry(level, log));
        }
    }

}