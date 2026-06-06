using System;
using System.Windows;
using System.Windows.Controls;

namespace ExhibitionManagementSystem.Desktop.Controls.Forms
{
    public partial class ExpoTextBox : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ExpoTextBox), 
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(string), typeof(ExpoTextBox), new PropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public ExpoTextBox()
        {
            InitializeComponent();
            UpdatePlaceholderVisibility();
            InputTextBox.GotFocus += (s, e) => UpdatePlaceholderVisibility();
            InputTextBox.LostFocus += (s, e) => UpdatePlaceholderVisibility();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExpoTextBox control)
            {
                control.UpdatePlaceholderVisibility();
            }
        }

        private void UpdatePlaceholderVisibility()
        {
            if (PlaceholderTextBlock == null || InputTextBox == null) return;

            bool hasText = !string.IsNullOrEmpty(InputTextBox.Text);
            bool isFocused = InputTextBox.IsFocused;

            PlaceholderTextBlock.Visibility = (hasText || isFocused) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
