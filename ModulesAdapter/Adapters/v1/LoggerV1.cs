using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.ModulesAdapter.Adapters.v1;

public class LoggerV1: OutfitTool.API1.Logger.ILogger
{
    ILogger _logger;
    public LoggerV1(ILogger logger)
    {
        this._logger = logger;
    }

    public void Critical(string log)
    {
        _logger.Critical(log);
    }

    public void Debug(string log)
    {
        _logger.Debug(log);
    }

    public void Error(string log)
    {
        _logger.Error(log);
    }

    public void Info(string log)
    {
        _logger.Info(log);
    }

    public void Status(string log)
    {
        _logger.Status(log);
    }

    public void Warning(string log)
    {
        _logger.Warning(log);
    }
}