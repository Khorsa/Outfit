using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using YamlDotNet.Core.Tokens;

namespace OutfitTool.Services.HotkeyManager
{
    internal class HotKeyRegisterService
    {
        // Импортируем методы из user32.dll
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Идентификаторы для горячих клавиш
        private static readonly Dictionary<int, HotKey> registredKeys = new();

        // Идентификатор для горячих клавиш
        private static int nextId = 0;

        // Переменные экземпляра
        private readonly IntPtr _windowHandle;

        public HotKeyRegisterService(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
        }

        public bool Register(HotKey key)
        {
            // Проверяем, не зарегистрирована ли уже такая клавиша
            if (!registredKeys.ContainsValue(key))
            {
                // Регистрируем
                bool result = RegisterHotKey(_windowHandle, nextId + 1, (uint)key.keyMod, (uint)KeyInterop.VirtualKeyFromKey(key.keyCode));

                if (!result)
                {
                    throw new Exception("Горячая клавиша " + key.ToString() + " уже зарегистрирована другим приложением.\r\nНазначте другую горячую клавишу.");
                }

                nextId++;
                registredKeys.Add(nextId, key);
                return true;
            }
            return false;
        }

        public void Unregister(HotKey key)
        {
            // Снимаем клавишу с регистрации
            foreach(KeyValuePair<int, HotKey> pair in registredKeys)
            {
                if (pair.Value == key)
                {
                    UnregisterHotKey(_windowHandle, pair.Key);
                }
            }
        }

        public void UnregisterAll()
        {
            foreach (KeyValuePair<int, HotKey> pair in registredKeys)
            {
                UnregisterHotKey(_windowHandle, pair.Key);
            }
        }

        public HotKey? getById(int Id)
        {
            HotKey? key = null;
            registredKeys.TryGetValue(Id, out key);
            return key;
        }

        public void Dispose()
        {
            UnregisterAll();
        }
    }
}
