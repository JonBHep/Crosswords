using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Crosswords;

public partial class CrosswordSizeDialogue : Window
{
    public CrosswordSizeDialogue()
    {
        InitializeComponent();
    }
    
    private int _dx = 0;
    private int _dy = 0;
    
    private void BoxX_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox bx = BoxX;
        int u;
        string q = bx.Text.Trim();
        if (int.TryParse(q, out int v))
        {
            bx.Foreground = Brushes.Blue;
            u = v;
        }
        else
        {
            bx.Foreground = Brushes.Red;
            u = 0;
        }
        _dx = u;
    }

    private void BoxY_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox bx = BoxY;
        int u;
        string q = bx.Text.Trim();
        if (int.TryParse(q, out int v))
        {
            bx.Foreground = Brushes.Blue;
            u = v;
        }
        else
        {
            bx.Foreground = Brushes.Red;
            u = 0;
        }
        _dy = u;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        bool er = _dx is < 4 or > 26 || _dy is < 4 or > 26;
        if (er)
        {
            MessageBox.Show("Values are missing or out of range (min 4, max 26)", Jbh.AppManager.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        DialogResult = true;
    }

    public int X_Dimension { get => _dx; }
    public int Y_Dimension { get => _dy; }
}