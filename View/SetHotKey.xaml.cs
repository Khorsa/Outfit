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

namespace OutfitTool;

/// <summary>
/// Логика взаимодействия для SetHotKey.xaml
/// </summary>
public partial class SetHotKey : Window
{
    private HotKey? _hotKey = null;

    public SetHotKey(string moduleName, string commandName, string commandDescription)
    {
        InitializeComponent();

        this.CommandDescription.Content = moduleName + "." + commandName + "\r\n(" + commandDescription + ")";
        this._hotKey = null;
    }

    public HotKey? GetPressedKey()
    {
        return this._hotKey;
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
            this._hotKey = new HotKey(e.Key, Keyboard.Modifiers);
            DialogResult = true;
        } else
        {
            DialogResult = false;
        }
        this.Close();
    }
}