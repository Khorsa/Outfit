namespace OutfitTool.ModulesAdapter.Logger;

public interface ILogger
{
    public void Subscribe(EventHandler<LogEntry> handler);
    void Debug(string log);
    void Info(string log);
    void Error(string log);
    void Warning(string log);
    void Critical(string log);
    void Status(string log);
}