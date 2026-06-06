using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace ExhibitionManagementSystem.Desktop.Controls.Charts
{
    public partial class AreaChartControl : UserControl
    {
        public static readonly DependencyProperty SeriesProperty =
            DependencyProperty.Register(nameof(Series), typeof(IEnumerable<ISeries>), typeof(AreaChartControl), new PropertyMetadata(null));

        public static readonly DependencyProperty XAxesProperty =
            DependencyProperty.Register(nameof(XAxes), typeof(IEnumerable<Axis>), typeof(AreaChartControl), new PropertyMetadata(null));

        public static readonly DependencyProperty YAxesProperty =
            DependencyProperty.Register(nameof(YAxes), typeof(IEnumerable<Axis>), typeof(AreaChartControl), new PropertyMetadata(null));

        public IEnumerable<ISeries> Series
        {
            get => (IEnumerable<ISeries>)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        public IEnumerable<Axis> XAxes
        {
            get => (IEnumerable<Axis>)GetValue(XAxesProperty);
            set => SetValue(XAxesProperty, value);
        }

        public IEnumerable<Axis> YAxes
        {
            get => (IEnumerable<Axis>)GetValue(YAxesProperty);
            set => SetValue(YAxesProperty, value);
        }

        public AreaChartControl()
        {
            InitializeComponent();
        }
    }
}
