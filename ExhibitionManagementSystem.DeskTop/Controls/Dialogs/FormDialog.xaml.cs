using System.Windows;

namespace ExhibitionManagementSystem.DeskTop.Controls.Dialogs;

public partial class FormDialog : Window
{
    public FormDialog(UIElement content, string title)
    {
        InitializeComponent();
        Title = title;
        DialogContent.Content = content;
    }
}
