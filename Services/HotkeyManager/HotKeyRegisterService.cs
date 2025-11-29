using System.Runtime.InteropServices;
using System.Windows.Input;

namespace OutfitTool.Services.HotkeyManager;

public class HotKeyRegisterService
{
    // Импортируем методы из user32.dll
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Идентификаторы для горячих клавиш
    private static readonly Dictionary<int, HotKey> RegistredKeys = new();

    // Идентификатор для горячих клавиш
    private static int _nextId = 0;

    // Переменные экземпляра
    private readonly IntPtr _windowHandle;

    public HotKeyRegisterService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public bool Register(HotKey key)
    {
        // Проверяем, не зарегистрирована ли уже такая клавиша
        if (!RegistredKeys.ContainsValue(key))
        {
            // Регистрируем
            bool result = RegisterHotKey(_windowHandle, _nextId + 1, (uint)key.KeyMod, (uint)KeyInterop.VirtualKeyFromKey(key.KeyCode));

            if (!result)
            {
                throw new Exception("Горячая клавиша " + key.ToString() + " уже зарегистрирована другим приложением.\r\nНазначте другую горячую клавишу.");
            }

            _nextId++;
            RegistredKeys.Add(_nextId, key);
            return true;
        }
        return false;
    }

    public void Unregister(HotKey key)
    {
        // Снимаем клавишу с регистрации
        foreach(KeyValuePair<int, HotKey> pair in RegistredKeys)
        {
            if (pair.Value == key)
            {
                UnregisterHotKey(_windowHandle, pair.Key);
            }
        }
    }

    public void UnregisterAll()
    {
        foreach (KeyValuePair<int, HotKey> pair in RegistredKeys)
        {
            UnregisterHotKey(_windowHandle, pair.Key);
        }
    }

    public HotKey? GetById(int id)
    {
        HotKey? key = null;
        RegistredKeys.TryGetValue(id, out key);
        return key;
    }

    public void Dispose()
    {
        UnregisterAll();
    }
}