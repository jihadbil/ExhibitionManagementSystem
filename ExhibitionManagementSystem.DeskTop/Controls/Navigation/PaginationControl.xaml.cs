using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExhibitionManagementSystem.DeskTop.Controls.Navigation;

public partial class PaginationControl : UserControl
{
    public static readonly DependencyProperty CurrentPageProperty =
        DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(PaginationControl), new PropertyMetadata(1));

    public static readonly DependencyProperty TotalPagesProperty =
        DependencyProperty.Register(nameof(TotalPages), typeof(int), typeof(PaginationControl), new PropertyMetadata(1));

    public static readonly DependencyProperty TotalCountProperty =
        DependencyProperty.Register(nameof(TotalCount), typeof(int), typeof(PaginationControl), new PropertyMetadata(0));

    public static readonly DependencyProperty UnitLabelProperty =
        DependencyProperty.Register(nameof(UnitLabel), typeof(string), typeof(PaginationControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrevCommandProperty =
        DependencyProperty.Register(nameof(PrevCommand), typeof(ICommand), typeof(PaginationControl), new PropertyMetadata(null));

    public static readonly DependencyProperty NextCommandProperty =
        DependencyProperty.Register(nameof(NextCommand), typeof(ICommand), typeof(PaginationControl), new PropertyMetadata(null));

    public int CurrentPage { get => (int)GetValue(CurrentPageProperty); set => SetValue(CurrentPageProperty, value); }
    public int TotalPages { get => (int)GetValue(TotalPagesProperty); set => SetValue(TotalPagesProperty, value); }
    public int TotalCount { get => (int)GetValue(TotalCountProperty); set => SetValue(TotalCountProperty, value); }
    public string UnitLabel { get => (string)GetValue(UnitLabelProperty); set => SetValue(UnitLabelProperty, value); }
    public ICommand PrevCommand { get => (ICommand)GetValue(PrevCommandProperty); set => SetValue(PrevCommandProperty, value); }
    public ICommand NextCommand { get => (ICommand)GetValue(NextCommandProperty); set => SetValue(NextCommandProperty, value); }

    public PaginationControl()
    {
        InitializeComponent();
    }
}
