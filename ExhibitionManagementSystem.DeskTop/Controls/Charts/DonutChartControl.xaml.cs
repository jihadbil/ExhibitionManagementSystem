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

public partial class DonutChartControl : UserControl
{
    public static readonly DependencyProperty SegmentsProperty =
        DependencyProperty.Register(nameof(Segments), typeof(IList<ExhibitionTypeChartItem>), typeof(DonutChartControl),
            new PropertyMetadata(null, OnSegmentsChanged));

    public IList<ExhibitionTypeChartItem>? Segments
    {
        get => (IList<ExhibitionTypeChartItem>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public static readonly DependencyProperty SeriesProperty =
        DependencyProperty.Register(nameof(Series), typeof(ISeries[]), typeof(DonutChartControl),
            new PropertyMetadata(null));

    public ISeries[] Series
    {
        get => (ISeries[])GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public DonutChartControl()
    {
        InitializeComponent();
    }

    private static void OnSegmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DonutChartControl ctrl && e.NewValue is IList<ExhibitionTypeChartItem> data)
        {
            ctrl.LoadData(data);
        }
    }

    public void LoadData(IList<ExhibitionTypeChartItem> data)
    {
        if (data == null || data.Count == 0) return;

        // Colors for donut slices
        var colors = new[]
        {
            new SKColor(99, 102, 241),   // #6366F1
            new SKColor(16, 185, 129),   // #10B981
            new SKColor(245, 158, 11),   // #F59E0B
            new SKColor(239, 68, 68),    // #EF4444
            new SKColor(139, 92, 246),   // #8B5CF6
            new SKColor(75, 85, 99)      // #4B5563
        };

        var seriesList = new List<ISeries>();
        for (int i = 0; i < data.Count; i++)
        {
            var item = data[i];
            var color = colors[i % colors.Length];

            seriesList.Add(new PieSeries<double>
            {
                Values = new double[] { item.Count },
                Name = item.Type,
                InnerRadius = 60,
                Fill = new SolidColorPaint(color)
            });
        }

        Series = seriesList.ToArray();
    }
}
