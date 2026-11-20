/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace OutfitTool.Services.HotkeyManager;

public class HotKeyRegisterService
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int ErrorHotKeyAlreadyRegistered = 1409;

    private readonly object _sync = new();
    private readonly Dictionary<int, HotKey> _registeredKeys = new();
    private readonly IntPtr _windowHandle;
    private int _nextId;

    public HotKeyRegisterService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    /// <summary>
    /// Регистрирует комбинацию. false — комбинация уже зарегистрирована (этим или другим сервисом).
    /// </summary>
    public bool Register(HotKey key)
    {
        lock (_sync)
        {
            if (_registeredKeys.Values.Any(existing => existing == key))
            {
                return false;
            }

            var id = NextId();
            if (!RegisterHotKey(_windowHandle, id, (uint)key.KeyMod,
                    (uint)KeyInterop.VirtualKeyFromKey(key.KeyCode)))
            {
                var error = Marshal.GetLastWin32Error();
                var message = error == ErrorHotKeyAlreadyRegistered
                    ? $"Горячая клавиша {key} уже зарегистрирована другим приложением."
                    : $"Не удалось зарегистрировать горячую клавишу {key} (код {error}).";

                throw new InvalidOperationException(message);
            }

            _registeredKeys[id] = key;
            return true;
        }
    }

    public void Unregister(HotKey key)
    {
        lock (_sync)
        {
            var ids = _registeredKeys
                .Where(pair => pair.Value == key)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var id in ids)
            {
                UnregisterHotKey(_windowHandle, id);
                _registeredKeys.Remove(id);
            }
        }
    }

    public void UnregisterAll()
    {
        lock (_sync)
        {
            foreach (var id in _registeredKeys.Keys.ToList())
            {
                UnregisterHotKey(_windowHandle, id);
            }

            _registeredKeys.Clear();
            _nextId = 0;
        }
    }

    public HotKey? GetById(int id)
    {
        lock (_sync)
        {
            return _registeredKeys.TryGetValue(id, out var key) ? key : null;
        }
    }

    public bool IsRegistered(HotKey key)
    {
        lock (_sync)
        {
            return _registeredKeys.Values.Any(existing => existing == key);
        }
    }

    public void Dispose()
    {
        UnregisterAll();
    }

    private int NextId()
    {
        if (_nextId == int.MaxValue) _nextId = 0;
        return ++_nextId;
    }
}
