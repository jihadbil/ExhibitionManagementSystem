using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExhibitionManagementSystem.DeskTop.Controls.Forms;

public partial class SearchBarControl : UserControl
{
    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(SearchBarControl),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(SearchBarControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SearchCommandProperty =
        DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(SearchBarControl), new PropertyMetadata(null));

    public static readonly DependencyProperty ButtonTextProperty =
        DependencyProperty.Register(nameof(ButtonText), typeof(string), typeof(SearchBarControl), new PropertyMetadata("بحث"));

    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public string PlaceholderText { get => (string)GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }
    public ICommand SearchCommand { get => (ICommand)GetValue(SearchCommandProperty); set => SetValue(SearchCommandProperty, value); }
    public string ButtonText { get => (string)GetValue(ButtonTextProperty); set => SetValue(ButtonTextProperty, value); }

    public SearchBarControl()
    {
        InitializeComponent();
    }
}
