/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Threading;
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
    private CancellationTokenSource? _cancellationTokenSource;

    public UpdateWindow(LastCompatibleRepositoryItem module)
    {
        this.Module = module;
        InitializeComponent();

        Closed += (_, _) =>
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        };
    }

    public void ShowDialog(string action)
    {
        var updatesManager = ServiceLocator.GetService<IUpdatesManagerInterface>();

        if (action == "delete")
        {
            CancelButton.Visibility = Visibility.Collapsed;
            UpdateStatus.Content = "Удаление модуля";
            updatesManager.DeleteModule(this.Module);
            base.ShowDialog();
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        Loaded += async (_, _) =>
        {
            try
            {
                UpdateStatus.Content = action == "install"
                    ? "Установка модуля..."
                    : "Обновление модуля...";

                await updatesManager.DownloadAsync(this.Module, token);
            }
            catch (OperationCanceledException)
            {
                UpdateStatus.Content = "Отменено";
            }
            catch (Exception ex)
            {
                UpdateStatus.Content = "Ошибка: " + ex.Message;
            }
            finally
            {
                if (IsVisible)
                {
                    Close();
                }
            }
        };

        base.ShowDialog();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        CancelButton.IsEnabled = false;
        UpdateStatus.Content = "Отмена...";
        _cancellationTokenSource?.Cancel();
    }
}
