using OutfitTool.Common;
using OutfitTool.Services;
using OutfitTool.View;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;

namespace OutfitTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ModuleInfoInterface ModuleInfo { get; private set; }

        public App() : base() {
            App.ModuleInfo = new ModuleInfo();
        }

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            ServiceLocator.RegisterServices();
            StartParameters.FillFromCommandLineArguments(e);
        }
    }
}
