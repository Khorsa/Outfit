using Common.Logger;
using OutfitTool.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OutfitTool.View
{
    /// <summary>
    /// Логика взаимодействия для TabLogs.xaml
    /// </summary>
    public partial class TabLogs : UserControl
    {
        public TabLogs()
        {
            InitializeComponent();
            ServiceLocator.GetService<LoggerInterface>().Subscribe(this.logHandler);
        }

        private void logHandler(object? sender, LogEntry logEntry)
        {
            string message = logEntry.LogDateTime.ToString() + " " + logEntry.Level.ToString() + ": " + logEntry.Message;
            logsBlock.Text += message + "\r\n";
        }
    }
}
