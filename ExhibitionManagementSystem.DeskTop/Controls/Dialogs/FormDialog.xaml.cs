using System.Windows;
using System.Windows.Input;

namespace ExhibitionManagementSystem.DeskTop.Controls.Dialogs;

public partial class FormDialog : Window
{
    public FormDialog(UIElement content, string title)
    {
        InitializeComponent();
        Title = title;
        DialogContent.Content = content;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

