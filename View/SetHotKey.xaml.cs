using OutfitTool.Services.HotkeyManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OutfitTool
{
    /// <summary>
    /// Логика взаимодействия для SetHotKey.xaml
    /// </summary>
    public partial class SetHotKey : Window
    {
        private HotKey? hotKey = null;

        public SetHotKey(string ModuleName, string CommandName, string CommandDescription)
        {
            InitializeComponent();

            this.CommandDescription.Content = ModuleName + "." + CommandName + "\r\n(" + CommandDescription + ")";
            this.hotKey = null;
        }

        public HotKey? getPressedKey()
        {
            return this.hotKey;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                this.hotKey = new HotKey(e.Key, Keyboard.Modifiers);
                DialogResult = true;
            } else
            {
                DialogResult = false;
            }
            this.Close();
        }
    }
}
