using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;

namespace OutfitTool.Services.HotkeyManager
{
    public class HotKey
    {
        public Key keyCode;
        public ModifierKeys keyMod;

        public HotKey(Key keyCode, ModifierKeys keyMod)
        {
            this.keyCode = keyCode;
            this.keyMod = keyMod;
        }

        public override string ToString() { 
            if (keyMod == ModifierKeys.None)
            {
                return keyCode.ToString();
            }
            else
            {
                return keyMod.ToString().Replace(" ", "") + '+' + keyCode.ToString();
            }
        }

        public static HotKey? FromString(string hotKeyString)
        {
            HotKey? result = null;

            string[] divided = hotKeyString.Trim().Split('+');
            Key key;
            ModifierKeys keyMod = ModifierKeys.None;

            if (
                divided.Length == 2 
                && Enum.TryParse<Key>(divided[1], out key) 
                && Enum.TryParse<ModifierKeys>(divided[0], out keyMod)
                ||
                divided.Length == 1 
                && Enum.TryParse<Key>(divided[0], out key)
                )
            {
                return new HotKey(key, keyMod);
            }

            return result;
        }

        public static bool operator ==(HotKey left, HotKey right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;

            return left.keyCode == right.keyCode && left.keyMod == right.keyMod;
        }

        public static bool operator !=(HotKey left, HotKey right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is HotKey other)
            {
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(keyCode, keyMod);
        }
    }
}
