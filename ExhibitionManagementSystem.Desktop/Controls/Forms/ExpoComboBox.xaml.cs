using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace ExhibitionManagementSystem.Desktop.Controls.Forms
{
    public partial class ExpoComboBox : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(ExpoComboBox), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(ExpoComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionChanged));

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register("SelectedValue", typeof(object), typeof(ExpoComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionChanged));

        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register("SelectedValuePath", typeof(string), typeof(ExpoComboBox), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(ExpoComboBox), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(string), typeof(ExpoComboBox), new PropertyMetadata(string.Empty));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public object SelectedValue
        {
            get => GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        public string SelectedValuePath
        {
            get => (string)GetValue(SelectedValuePathProperty);
            set => SetValue(SelectedValuePathProperty, value);
        }

        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public ExpoComboBox()
        {
            InitializeComponent();
            UpdatePlaceholderVisibility();
            InputComboBox.SelectionChanged += (s, e) => UpdatePlaceholderVisibility();
        }

        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExpoComboBox control)
            {
                control.UpdatePlaceholderVisibility();
            }
        }

        private void UpdatePlaceholderVisibility()
        {
            if (PlaceholderTextBlock == null || InputComboBox == null) return;

            bool hasSelection = InputComboBox.SelectedItem != null;
            PlaceholderTextBlock.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
