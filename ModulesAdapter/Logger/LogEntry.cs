namespace OutfitTool.ModulesAdapter.Logger;

public class LogEntry(LogLevel level, string message) : EventArgs
{
    public readonly LogLevel Level = level;
    public readonly string Message = message;
    public DateTime LogDateTime = DateTime.Now;
}