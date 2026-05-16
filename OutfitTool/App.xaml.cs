using OutfitTool.ModulesAdapter;
using OutfitTool.Services;
using System.Windows;
using OutfitTool.Services.Updates;

namespace OutfitTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public static ModuleInfo ModuleInfo { get; private set; } = null!;

    public App() : base() {
        ModuleInfo = new ModuleInfo();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ServiceLocator.RegisterServices();
        StartParameters.FillFromCommandLineArguments(e);

        if (!StartParameters.GetOne<bool>("update_mode")) return;
        
        // Update mode.
        var updater = ServiceLocator.GetService<FilesUpdater>();
        updater.Update(StartParameters.ModulesToDelete, StartParameters.GetAll());
    }
}