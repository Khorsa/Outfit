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
using System.Windows.Threading;

namespace OutfitTool.Services;

/// <summary>
/// Один экземпляр на пользовательскую сессию. Второй экземпляр будит первый
/// именованным событием и завершается.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\OutfitTool.SingleInstance";
    private const string ActivateEventName = @"Local\OutfitTool.Activate";

    private Mutex? _mutex;
    private EventWaitHandle? _activateEvent;
    private Thread? _listener;
    private volatile bool _running;

    public bool TryAcquire()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            SignalExistingInstance();
            _mutex.Dispose();
            _mutex = null;
            return false;
        }

        _running = true;
        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
        _listener = new Thread(ListenForActivation)
        {
            IsBackground = true,
            Name = "OutfitTool.SingleInstance"
        };
        _listener.Start();
        return true;
    }

    private static void SignalExistingInstance()
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(ActivateEventName, out var handle))
            {
                using (handle)
                {
                    handle.Set();
                }
            }
        }
        catch
        {
            // Если первый экземпляр ещё не создал событие, второй просто завершится.
        }
    }

    private void ListenForActivation()
    {
        while (_running)
        {
            try
            {
                _activateEvent?.WaitOne();
                if (!_running) return;

                var application = Application.Current;
                if (application == null) continue;

                application.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    new Action(() =>
                    {
                        var window = OutfitTool.View.MainWindow.Instance;
                        if (window == null) return;

                        window.Show();
                        if (window.WindowState == WindowState.Minimized)
                        {
                            window.WindowState = WindowState.Normal;
                        }

                        window.Activate();
                        window.Topmost = true;
                        window.Topmost = false;
                        window.Focus();
                    }));
            }
            catch
            {
                // Слушатель не должен ронять приложение.
            }
        }
    }

    public void Dispose()
    {
        _running = false;

        try { _activateEvent?.Set(); } catch { /* ignored */ }
        _listener?.Join(TimeSpan.FromSeconds(1));

        _activateEvent?.Dispose();
        _activateEvent = null;

        if (_mutex != null)
        {
            try { _mutex.ReleaseMutex(); } catch { /* ignored */ }
            _mutex.Dispose();
            _mutex = null;
        }
    }
}
