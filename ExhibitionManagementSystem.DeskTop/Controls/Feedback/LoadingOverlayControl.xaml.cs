using System.Windows;
using System.Windows.Controls;

namespace ExhibitionManagementSystem.DeskTop.Controls.Feedback;

public partial class LoadingOverlayControl : UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadingOverlayControl), new PropertyMetadata("جاري التحميل..."));

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(LoadingOverlayControl), new PropertyMetadata(false));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public LoadingOverlayControl()
    {
        InitializeComponent();
    }
}
