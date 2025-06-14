using Common.Logger;
using OutfitTool.Services;
using System.IO;
using System.Windows.Controls;

namespace OutfitTool.View
{
    /// <summary>
    /// Логика взаимодействия для TabLogs.xaml
    /// </summary>
    public partial class TabLogs : UserControl
    {
        private const bool LOG_IN_FILE = false;
        public TabLogs()
        {
            InitializeComponent();

            ServiceLocator.GetService<LoggerInterface>().Subscribe(this.logHandler);

            if (LOG_IN_FILE){
                ServiceLocator.GetService<LoggerInterface>().Subscribe(this.logFileHandler);
            }
        }

        private void logHandler(object? sender, LogEntry logEntry)
        {
            string message = logEntry.LogDateTime.ToString() + " " + logEntry.Level.ToString() + ": " + logEntry.Message;
            logsBlock.Text += message + "\r\n";
        }

        private void logFileHandler(object? sender, LogEntry logEntry)
        {
            var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            string message = logEntry.LogDateTime.ToString() + " " + logEntry.Level.ToString() + ": " + logEntry.Message;
            File.WriteAllTextAsync(file, message);
        }
    }
}
