using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExhibitionManagementSystem.Desktop.Controls.Forms
{
    public partial class EmptyStateControl : UserControl
    {
        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register(nameof(IconKind), typeof(string), typeof(EmptyStateControl), new PropertyMetadata("AlertCircleOutline"));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyStateControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(EmptyStateControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(EmptyStateControl), new PropertyMetadata(null));

        public static readonly DependencyProperty ActionCommandProperty =
            DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(EmptyStateControl), new PropertyMetadata(null));

        public string IconKind
        {
            get => (string)GetValue(IconKindProperty);
            set => SetValue(IconKindProperty, value);
        }

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

        public string? ActionText
        {
            get => (string?)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public ICommand? ActionCommand
        {
            get => (ICommand?)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        public EmptyStateControl()
        {
            InitializeComponent();
        }
    }
}
