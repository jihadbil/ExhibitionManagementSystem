using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExhibitionManagementSystem.Desktop.Controls.Dialogs
{
    public partial class BaseDialog : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(BaseDialog), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DialogContentProperty =
            DependencyProperty.Register("DialogContent", typeof(object), typeof(BaseDialog), new PropertyMetadata(null));

        public static readonly DependencyProperty DialogWidthProperty =
            DependencyProperty.Register("DialogWidth", typeof(double), typeof(BaseDialog), new PropertyMetadata(450.0));

        public static readonly DependencyProperty DialogHeightProperty =
            DependencyProperty.Register("DialogHeight", typeof(double), typeof(BaseDialog), new PropertyMetadata(double.NaN));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public object DialogContent
        {
            get => GetValue(DialogContentProperty);
            set => SetValue(DialogContentProperty, value);
        }

        public double DialogWidth
        {
            get => (double)GetValue(DialogWidthProperty);
            set => SetValue(DialogWidthProperty, value);
        }

        public double DialogHeight
        {
            get => (double)GetValue(DialogHeightProperty);
            set => SetValue(DialogHeightProperty, value);
        }

        public event EventHandler? Closed;

        public BaseDialog()
        {
            InitializeComponent();
        }

        public void Show()
        {
            Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            Visibility = Visibility.Collapsed;
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                Hide();
            }
        }
    }
}
