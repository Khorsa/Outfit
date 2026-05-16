/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services;
using OutfitTool.Services.Settings;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using OutfitTool.ModulesAdapter;
using OutfitTool.ModulesAdapter.Logger;
using Module = OutfitTool.ModulesAdapter.ModuleManager.Module;

namespace OutfitTool.View;

/// <summary>
/// Логика взаимодействия для TabModules.xaml
/// </summary>
public partial class TabModules
{
    public TabModules()
    {
        InitializeComponent();
    }

    private void moduleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (moduleList.SelectedItem is ListViewModuleItem selectedModule)
        {
            SetButtons(selectedModule.Module);
        }
        else
        {
            switchModule.IsEnabled = false;
            moduleSettings.IsEnabled = false;
        }
    }

    private void SetButtons(Module module)
    {
        switchModule.IsEnabled = true;
        moduleSettings.IsEnabled = true;

        switchModule.Content = LocalizationHelper.GetString(module.Enabled ? "Disable" : "Enable");
    }

    private void switchModule_Click_1(object sender, RoutedEventArgs e)
    {
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

        if (moduleList.SelectedItem is ListViewModuleItem selectedModule)
        {
            selectedModule.Module.Enabled = !selectedModule.Module.Enabled;

            var settings = settingsManager.LoadSettings();

            if (selectedModule.Module.Enabled)
            {
                settings.EnabledModules.Add(selectedModule.Module.AssemblyName);
            }
            else
            {
                settings.EnabledModules.Remove(selectedModule.Module.AssemblyName);
            }
            settingsManager.SaveSettings(settings);

            InitModules();
            InitInterface();
        }
    }

    private void InitModules()
    {
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
        var moduleManager = ServiceLocator.GetService<IModuleManager>();

        // Инициализация модулей
        var settings = settingsManager.LoadSettings();
        moduleManager.LoadModules(settings.EnabledModules);
        foreach (Module module in moduleManager.GetModules())
        {
            if (module.Enabled)
            {
                module.ModuleController.Init(ServiceLocator.GetService<ILogger>());
            }
        }
    }

    private void InitInterface()
    {
        var moduleManager = ServiceLocator.GetService<IModuleManager>();
        var modules = moduleManager.GetModules();

        // Инициализация полного списка модулей
        var lvModuleItems = new ObservableCollection<ListViewModuleItem>();
        foreach (var module in modules)
        {
            lvModuleItems.Add(new ListViewModuleItem(module));
        }
        ListViewModuleItem? selectedModule = null;
        if (moduleList.SelectedItem is ListViewModuleItem s)
        {
            selectedModule = s;
        }
        moduleList.ItemsSource = lvModuleItems;
        if (selectedModule != null)
        {
            foreach (ListViewModuleItem m in moduleList.Items)
            {
                if (m.Name == selectedModule.Name)
                {
                    moduleList.SelectedItem = m;
                    break;
                }
            }
        }
        MainWindow.Instance?.InitInterface();
    }


    private void moduleSettings_Click(object sender, RoutedEventArgs e)
    {
        if (moduleList.SelectedItem is ListViewModuleItem selectedModule)
        {
            selectedModule.Module.ModuleController.OpenSettings();
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        InitInterface();
    }
}