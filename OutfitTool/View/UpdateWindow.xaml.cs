/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
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