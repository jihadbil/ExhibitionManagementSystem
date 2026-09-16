using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExhibitionManagementSystem.DeskTop.Controls.Feedback;

public partial class EmptyStateControl : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyStateControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(EmptyStateControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconGeometryProperty =
        DependencyProperty.Register(nameof(IconGeometry), typeof(Geometry), typeof(EmptyStateControl), new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public Geometry IconGeometry
    {
        get => (Geometry)GetValue(IconGeometryProperty);
        set => SetValue(IconGeometryProperty, value);
    }

    public EmptyStateControl()
    {
        InitializeComponent();
    }
}
