using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExhibitionManagementSystem.Desktop.Controls.Dialogs
{
    public partial class FormDialog : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(FormDialog), new PropertyMetadata("نموذج"));

        public static readonly DependencyProperty FormContentProperty =
            DependencyProperty.Register("FormContent", typeof(object), typeof(FormDialog), new PropertyMetadata(null));

        public static readonly DependencyProperty SubmitButtonTextProperty =
            DependencyProperty.Register("SubmitButtonText", typeof(string), typeof(FormDialog), new PropertyMetadata("حفظ"));

        public static readonly DependencyProperty SubmitCommandProperty =
            DependencyProperty.Register("SubmitCommand", typeof(ICommand), typeof(FormDialog), new PropertyMetadata(null));

        public static readonly DependencyProperty DialogWidthProperty =
            DependencyProperty.Register("DialogWidth", typeof(double), typeof(FormDialog), new PropertyMetadata(500.0));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public object FormContent
        {
            get => GetValue(FormContentProperty);
            set => SetValue(FormContentProperty, value);
        }

        public string SubmitButtonText
        {
            get => (string)GetValue(SubmitButtonTextProperty);
            set => SetValue(SubmitButtonTextProperty, value);
        }

        public ICommand SubmitCommand
        {
            get => (ICommand)GetValue(SubmitCommandProperty);
            set => SetValue(SubmitCommandProperty, value);
        }

        public double DialogWidth
        {
            get => (double)GetValue(DialogWidthProperty);
            set => SetValue(DialogWidthProperty, value);
        }

        public event EventHandler? Submitted;
        public event EventHandler? Cancelled;

        public FormDialog()
        {
            InitializeComponent();
            UnderlyingDialog.Closed += UnderlyingDialog_Closed;
        }

        public void Show()
        {
            Visibility = Visibility.Visible;
            UnderlyingDialog.Show();
        }

        public void Hide()
        {
            UnderlyingDialog.Hide();
            Visibility = Visibility.Collapsed;
        }

        private void UnderlyingDialog_Closed(object? sender, EventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Submitted?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            Hide();
        }
    }
}
