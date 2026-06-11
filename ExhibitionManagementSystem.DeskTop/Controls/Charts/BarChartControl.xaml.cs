using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ExhibitionManagementSystem.Models.DTOs.Dashboard;

namespace ExhibitionManagementSystem.DeskTop.Controls.Charts;

public partial class BarChartControl : UserControl
{
    public static readonly DependencyProperty SeriesDataProperty =
        DependencyProperty.Register(nameof(SeriesData), typeof(IList<RevenueChartPointDto>), typeof(BarChartControl),
            new PropertyMetadata(null, OnSeriesDataChanged));

    public IList<RevenueChartPointDto>? SeriesData
    {
        get => (IList<RevenueChartPointDto>?)GetValue(SeriesDataProperty);
        set => SetValue(SeriesDataProperty, value);
    }

    public static readonly DependencyProperty SeriesProperty =
        DependencyProperty.Register(nameof(Series), typeof(ISeries[]), typeof(BarChartControl),
            new PropertyMetadata(null));

    public ISeries[] Series
    {
        get => (ISeries[])GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public static readonly DependencyProperty XAxesProperty =
        DependencyProperty.Register(nameof(XAxes), typeof(Axis[]), typeof(BarChartControl),
            new PropertyMetadata(null));

    public Axis[] XAxes
    {
        get => (Axis[])GetValue(XAxesProperty);
        set => SetValue(XAxesProperty, value);
    }

    public static readonly DependencyProperty YAxesProperty =
        DependencyProperty.Register(nameof(YAxes), typeof(Axis[]), typeof(BarChartControl),
            new PropertyMetadata(null));

    public Axis[] YAxes
    {
        get => (Axis[])GetValue(YAxesProperty);
        set => SetValue(YAxesProperty, value);
    }

    public BarChartControl()
    {
        InitializeComponent();
    }

    private static void OnSeriesDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BarChartControl ctrl && e.NewValue is IList<RevenueChartPointDto> data)
        {
            ctrl.LoadData(data);
        }
    }

    public void LoadData(IList<RevenueChartPointDto> data)
    {
        if (data == null || data.Count == 0) return;

        Series = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = data.Select(d => d.Revenue).ToArray(),
                Fill = new LinearGradientPaint(
                    new SKColor(99, 102, 241),   // #6366F1
                    new SKColor(139, 92, 246)     // #8B5CF6
                ),
                Rx = 8, // Corner Radius top
                Ry = 8,
                Name = "الإيرادات"
            }
        };

        XAxes = new Axis[]
        {
            new Axis
            {
                Labels = data.Select(d => d.Month).ToArray(),
                TextSize = 12
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                TextSize = 12
            }
        };
    }
}
