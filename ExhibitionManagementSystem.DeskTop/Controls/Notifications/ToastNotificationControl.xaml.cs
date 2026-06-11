using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ExhibitionManagementSystem.DeskTop.Services.Notifications;

namespace ExhibitionManagementSystem.DeskTop.Controls.Notifications;

public partial class ToastNotificationControl : UserControl
{
    private readonly Queue<NotificationEventArgs> _queue = new();
    private bool _isShowing = false;
    private INotificationService? _notificationService;

    public ToastNotificationControl()
    {
        InitializeComponent();
    }

    public void Initialize(INotificationService notificationService)
    {
        _notificationService = notificationService;
        _notificationService.NotificationRequested += OnNotificationRequested;
    }

    private void OnNotificationRequested(object? sender, NotificationEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _queue.Enqueue(e);
            if (!_isShowing) ShowNext();
        });
    }

    private void ShowNext()
    {
        if (!_queue.TryDequeue(out var args))
        {
            _isShowing = false;
            this.Visibility = Visibility.Collapsed;
            return;
        }

        _isShowing = true;
        this.Visibility = Visibility.Visible;

        // Set content
        TitleText.Text = args.Title;
        MessageText.Text = args.Message;

        // Apply colors and icons based on notification type
        if (Application.Current != null)
        {
            switch (args.Type)
            {
                case NotificationType.Success:
                    ToastBorder.BorderBrush = (Brush)Application.Current.Resources["SuccessBrush"];
                    IconText.Text = "✓";
                    IconText.Foreground = (Brush)Application.Current.Resources["SuccessBrush"];
                    break;
                case NotificationType.Error:
                    ToastBorder.BorderBrush = (Brush)Application.Current.Resources["DangerBrush"];
                    IconText.Text = "✕";
                    IconText.Foreground = (Brush)Application.Current.Resources["DangerBrush"];
                    break;
                case NotificationType.Warning:
                    ToastBorder.BorderBrush = (Brush)Application.Current.Resources["WarningBrush"];
                    IconText.Text = "⚠";
                    IconText.Foreground = (Brush)Application.Current.Resources["WarningBrush"];
                    break;
                case NotificationType.Info:
                    ToastBorder.BorderBrush = (Brush)Application.Current.Resources["InfoBrush"];
                    IconText.Text = "ℹ";
                    IconText.Foreground = (Brush)Application.Current.Resources["InfoBrush"];
                    break;
            }
        }

        // Animate Slide In / Fade In
        this.Opacity = 0;
        
        var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        this.BeginAnimation(OpacityProperty, fadeInAnimation);

        // Auto-dismiss after 5 seconds
        Task.Delay(5000).ContinueWith(_ =>
        {
            Application.Current.Dispatcher.Invoke(Dismiss);
        });
    }

    private void Dismiss()
    {
        if (!_isShowing) return;

        var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        
        fadeOutAnimation.Completed += (s, e) =>
        {
            _isShowing = false;
            ShowNext();
        };

        this.BeginAnimation(OpacityProperty, fadeOutAnimation);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }
}
