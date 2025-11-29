using System.Windows;
using OutfitTool.Services;
using OutfitTool.Services.Updates;

namespace OutfitTool.View;

/// <summary>
/// Логика взаимодействия для Update.xaml
/// </summary>
public partial class UpdateWindow : Window
{
    public readonly LastCompatibleRepositoryItem Module;
    public UpdateWindow(LastCompatibleRepositoryItem module)
    {
        this.Module = module;
        InitializeComponent();
    }

    public void ShowDialog(string action)
    {
        var updatesManager = ServiceLocator.GetService<IUpdatesManagerInterface>();

        if (action == "update")
        {
            this.UpdateStatus.Content = "Обновление модуля";
            updatesManager.Download(this.Module);
        }
        if (action == "install")
        {
            this.UpdateStatus.Content = "Установка модуля";
            updatesManager.Download(this.Module);
        }
        if (action == "delete")
        {
            this.UpdateStatus.Content = "Удаление модуля";
            updatesManager.DeleteModule(this.Module);
        }
        base.ShowDialog();
    }
}