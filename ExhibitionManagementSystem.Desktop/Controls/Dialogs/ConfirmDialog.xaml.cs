using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExhibitionManagementSystem.Desktop.Controls.Dialogs
{
    public partial class ConfirmDialog : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ConfirmDialog), new PropertyMetadata("تأكيد العملية"));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(ConfirmDialog), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsDangerProperty =
            DependencyProperty.Register("IsDanger", typeof(bool), typeof(ConfirmDialog), new PropertyMetadata(true, OnIsDangerChanged));

        public static readonly DependencyProperty ConfirmButtonBrushProperty =
            DependencyProperty.Register("ConfirmButtonBrush", typeof(Brush), typeof(ConfirmDialog), new PropertyMetadata(null));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public bool IsDanger
        {
            get => (bool)GetValue(IsDangerProperty);
            set => SetValue(IsDangerProperty, value);
        }

        public Brush ConfirmButtonBrush
        {
            get => (Brush)GetValue(ConfirmButtonBrushProperty);
            set => SetValue(ConfirmButtonBrushProperty, value);
        }

        public event EventHandler? Confirmed;
        public event EventHandler? Cancelled;

        public ConfirmDialog()
        {
            InitializeComponent();
            UnderlyingDialog.Closed += UnderlyingDialog_Closed;
            UpdateConfirmButtonStyle();
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

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmed?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private static void OnIsDangerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ConfirmDialog dialog)
            {
                dialog.UpdateConfirmButtonStyle();
            }
        }

        private void UpdateConfirmButtonStyle()
        {
            if (IsDanger)
            {
                var dangerBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0.5),
                    EndPoint = new Point(1, 0.5)
                };
                dangerBrush.GradientStops.Add(new GradientStop(Color.FromRgb(239, 68, 68), 0.0)); // EF4444
                dangerBrush.GradientStops.Add(new GradientStop(Color.FromRgb(220, 38, 38), 1.0)); // DC2626
                dangerBrush.Freeze();
                ConfirmButtonBrush = dangerBrush;
            }
            else
            {
                ConfirmButtonBrush = Application.Current.TryFindResource("PrimaryGradientBrush") as Brush;
            }
        }
    }
}
