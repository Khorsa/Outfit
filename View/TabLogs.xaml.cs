using OutfitTool.Services;
using System.IO;
using System.Windows.Controls;
using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.View;

/// <summary>
/// Логика взаимодействия для TabLogs.xaml
/// </summary>
public partial class TabLogs : UserControl
{
    private const bool LogInFile = true;
    public TabLogs()
    {
        InitializeComponent();

        ServiceLocator.GetService<ILogger>().Subscribe(this.LogHandler);

        if (LogInFile){
            ServiceLocator.GetService<ILogger>().Subscribe(this.LogFileHandler);
        }
    }

    private void LogHandler(object? sender, LogEntry logEntry)
    {
        string message = logEntry.LogDateTime.ToString() + " " + logEntry.Level.ToString() + ": " + logEntry.Message;
        this.Dispatcher.Invoke(() => {
            logsBlock.Text += message + "\r\n";
        });

            
    }

    private void LogFileHandler(object? sender, LogEntry logEntry)
    {
        var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Outfit.log");
        string message = logEntry.LogDateTime.ToString() + " " + logEntry.Level.ToString() + ": " + logEntry.Message;
        File.WriteAllTextAsync(file, message);
    }
}