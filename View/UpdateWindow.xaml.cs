using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using OutfitTool.Services;
using OutfitTool.Services.Updates;
using OutfitTool.ModuleManager;

namespace OutfitTool.View
{
    /// <summary>
    /// Логика взаимодействия для Update.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        internal readonly LastCompatibleRepositoryItem module;
        internal UpdateWindow(LastCompatibleRepositoryItem module)
        {
            this.module = module;
            InitializeComponent();
        }

        public void ShowDialog(string action)
        {
            var updatesManager = ServiceLocator.GetService<UpdatesManagerInterface>();

            if (action == "update")
            {
                this.UpdateStatus.Content = "Обновление модуля";
                updatesManager.Download(this.module);
            }
            if (action == "install")
            {
                this.UpdateStatus.Content = "Установка модуля";
                updatesManager.Download(this.module);
            }
            if (action == "delete")
            {
                this.UpdateStatus.Content = "Удаление модуля";
                updatesManager.DeleteModule(this.module);
            }
            base.ShowDialog();
        }
    }
}
