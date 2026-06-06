using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;

namespace ExhibitionManagementSystem.Desktop.Controls.Charts
{
    public partial class DonutChartControl : UserControl
    {
        public static readonly DependencyProperty SeriesProperty =
            DependencyProperty.Register(nameof(Series), typeof(IEnumerable<ISeries>), typeof(DonutChartControl), new PropertyMetadata(null));

        public IEnumerable<ISeries> Series
        {
            get => (IEnumerable<ISeries>)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        public DonutChartControl()
        {
            InitializeComponent();
        }
    }
}
