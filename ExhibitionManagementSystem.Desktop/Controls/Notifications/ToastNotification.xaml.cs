using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ExhibitionManagementSystem.Desktop.Services.Notifications;
using MaterialDesignThemes.Wpf;

namespace ExhibitionManagementSystem.Desktop.Controls.Notifications
{
    public partial class ToastNotification : UserControl
    {
        public event EventHandler? Dismissed;

        public string Message { get; }
        public string? Description { get; }
        public NotificationType Type { get; }

        public ToastNotification(NotificationType type, string message, string? description)
        {
            InitializeComponent();
            Type = type;
            Message = message;
            Description = description;

            TitleTextBlock.Text = message;
            DescriptionTextBlock.Text = description;
            DescriptionTextBlock.Visibility = string.IsNullOrEmpty(description) ? Visibility.Collapsed : Visibility.Visible;

            ConfigureType();

            Loaded += ToastNotification_Loaded;
        }

        private void ConfigureType()
        {
            switch (Type)
            {
                case NotificationType.Success:
                    TypeIcon.Kind = PackIconKind.CheckCircleOutline;
                    TypeIcon.Foreground = Application.Current.TryFindResource("BrushSuccess") as Brush ?? Brushes.Green;
                    ProgressBarBrush.Color = Application.Current.TryFindResource("SuccessColor") is Color c1 ? c1 : Color.FromRgb(16, 185, 129);
                    break;
                case NotificationType.Error:
                    TypeIcon.Kind = PackIconKind.CloseCircleOutline;
                    TypeIcon.Foreground = Application.Current.TryFindResource("BrushDanger") as Brush ?? Brushes.Red;
                    ProgressBarBrush.Color = Application.Current.TryFindResource("DangerColor") is Color c2 ? c2 : Color.FromRgb(239, 68, 68);
                    break;
                case NotificationType.Warning:
                    TypeIcon.Kind = PackIconKind.AlertOutline;
                    TypeIcon.Foreground = Application.Current.TryFindResource("BrushWarning") as Brush ?? Brushes.Orange;
                    ProgressBarBrush.Color = Application.Current.TryFindResource("WarningColor") is Color c3 ? c3 : Color.FromRgb(245, 158, 11);
                    break;
                case NotificationType.Info:
                    TypeIcon.Kind = PackIconKind.InformationOutline;
                    TypeIcon.Foreground = Application.Current.TryFindResource("BrushInfo") as Brush ?? Brushes.Blue;
                    ProgressBarBrush.Color = Application.Current.TryFindResource("InfoColor") is Color c4 ? c4 : Color.FromRgb(59, 130, 246);
                    break;
            }
        }

        private void ToastNotification_Loaded(object sender, RoutedEventArgs e)
        {
            if (Resources["SlideInStoryboard"] is Storyboard slideIn)
            {
                slideIn.Begin(this);
            }
        }

        public void Dismiss()
        {
            if (Resources["FadeOutStoryboard"] is Storyboard fadeOut)
            {
                fadeOut.Completed += (s, e) => Dismissed?.Invoke(this, EventArgs.Empty);
                fadeOut.Begin(this);
            }
            else
            {
                Dismissed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Dismiss();
        }
    }
}
