using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ExhibitionManagementSystem.Models.DTOs.Badge;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.DeskTop.Services.Printing;

public class BadgePrintService
{
    /// <summary>
    /// طباعة الشارة مباشرة على الطابعة الافتراضية أو عبر نافذة حوار الطباعة.
    /// </summary>
    public bool PrintBadge(BadgePrintPayloadDto payload, bool showDialog = false)
    {
        try
        {
            var printDialog = new PrintDialog();
            if (showDialog)
            {
                if (printDialog.ShowDialog() != true)
                    return false;
            }

            var visual = CreateBadgeVisual(payload);
            
            // Measure and arrange visual according to badge dimensions (in 96 DPI points: 1mm ~ 3.78px)
            double widthPx = payload.WidthMm * 3.7795;
            double heightPx = payload.HeightMm * 3.7795;

            if (payload.Orientation == BadgeOrientation.Horizontal)
            {
                (widthPx, heightPx) = (heightPx, widthPx);
            }

            visual.Width = widthPx;
            visual.Height = heightPx;
            visual.Measure(new Size(widthPx, heightPx));
            visual.Arrange(new Rect(0, 0, widthPx, heightPx));
            visual.UpdateLayout();

            printDialog.PrintVisual(visual, $"Badge — {payload.FullName}");
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ أثناء إرسال الشارة للطابعة: {ex.Message}", "خطأ في الطباعة", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>
    /// بناء عنصر WPF مرئي (Visual) يمثل بطاقة الدخول بتصميم عصري وأبعاد دقيقة.
    /// </summary>
    public Border CreateBadgeVisual(BadgePrintPayloadDto payload)
    {
        var mainBrush = GetColorBrush(payload.HeaderBgColor, "#6366F1");
        var accentBrush = GetColorBrush(payload.AccentColor, "#4F46E5");
        var headerTextBrush = GetColorBrush(payload.HeaderTextColor, "#FFFFFF");

        var rootBorder = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });  // Top Header Banner
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Body
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(35) });  // Footer Bar

        // 1. Top Header Banner
        var headerBorder = new Border
        {
            Background = mainBrush,
            Padding = new Thickness(12, 8, 12, 8)
        };
        Grid.SetRow(headerBorder, 0);

        var headerStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var categoryText = new TextBlock
        {
            Text = payload.ParticipantTypeTitle,
            Foreground = headerTextBrush,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var exhibitionText = new TextBlock
        {
            Text = payload.ExhibitionName,
            Foreground = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };
        headerStack.Children.Add(categoryText);
        headerStack.Children.Add(exhibitionText);
        headerBorder.Child = headerStack;
        grid.Children.Add(headerBorder);

        // 2. Middle Body
        var bodyStack = new StackPanel
        {
            Margin = new Thickness(16, 12, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetRow(bodyStack, 1);

        // Attendee Full Name
        var nameText = new TextBlock
        {
            Text = payload.FullName,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(26, 29, 41)),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4)
        };
        bodyStack.Children.Add(nameText);

        // Company Name
        if (!string.IsNullOrWhiteSpace(payload.CompanyName))
        {
            var companyText = new TextBlock
            {
                Text = payload.CompanyName,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = accentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            };
            bodyStack.Children.Add(companyText);
        }

        // Job Title
        if (!string.IsNullOrWhiteSpace(payload.JobTitle))
        {
            var titleText = new TextBlock
            {
                Text = payload.JobTitle,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            bodyStack.Children.Add(titleText);
        }

        // QR Code Container / Graphic placeholder
        var qrBorder = new Border
        {
            Width = 110,
            Height = 110,
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 4, 0, 6),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var qrStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        var qrIcon = new TextBlock
        {
            Text = "📱 QR CODE",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = accentBrush,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var qrCodeValue = new TextBlock
        {
            Text = payload.QRCode.Length > 16 ? payload.QRCode[..16] + "..." : payload.QRCode,
            FontSize = 8,
            FontFamily = new FontFamily("Consolas"),
            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };
        qrStack.Children.Add(qrIcon);
        qrStack.Children.Add(qrCodeValue);
        qrBorder.Child = qrStack;
        bodyStack.Children.Add(qrBorder);

        // Location / Booth Number if present
        if (!string.IsNullOrWhiteSpace(payload.BoothNumber) || !string.IsNullOrWhiteSpace(payload.VenueAndHallName))
        {
            var locText = new TextBlock
            {
                Text = !string.IsNullOrWhiteSpace(payload.BoothNumber) 
                    ? $"جناح رقم: {payload.BoothNumber} | {payload.VenueAndHallName}"
                    : payload.VenueAndHallName,
                FontSize = 10,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };
            bodyStack.Children.Add(locText);
        }

        grid.Children.Add(bodyStack);

        // 3. Footer Bar
        var footerBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(8, 4, 8, 4)
        };
        Grid.SetRow(footerBorder, 2);

        var footerText = new TextBlock
        {
            Text = payload.DatesText ?? payload.FooterText ?? "ExpoManager Security Verified",
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        footerBorder.Child = footerText;
        grid.Children.Add(footerBorder);

        rootBorder.Child = grid;
        return rootBorder;
    }

    private static SolidColorBrush GetColorBrush(string? hex, string fallbackHex)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex))
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
        }
        catch { }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallbackHex));
    }
}
