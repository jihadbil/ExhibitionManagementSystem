using System;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace ExhibitionManagementSystem.Desktop.Controls.Forms
{
    public partial class ExpoPasswordBox : UserControl
    {
        private bool _isUpdating;
        private bool _showPassword;

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string), typeof(ExpoPasswordBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(string), typeof(ExpoPasswordBox), new PropertyMetadata(string.Empty));

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public ExpoPasswordBox()
        {
            InitializeComponent();
            UpdatePlaceholderVisibility();

            MaskedPasswordBox.PasswordChanged += MaskedPasswordBox_PasswordChanged;
            UnmaskedTextBox.TextChanged += UnmaskedTextBox_TextChanged;

            MaskedPasswordBox.GotFocus += InputControl_GotFocus;
            MaskedPasswordBox.LostFocus += InputControl_LostFocus;
            UnmaskedTextBox.GotFocus += InputControl_GotFocus;
            UnmaskedTextBox.LostFocus += InputControl_LostFocus;
        }

        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExpoPasswordBox control)
            {
                control.SyncPasswordFromProperty();
                control.UpdatePlaceholderVisibility();
            }
        }

        private void MaskedPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;
            Password = MaskedPasswordBox.Password;
            UnmaskedTextBox.Text = Password;
            _isUpdating = false;
            UpdatePlaceholderVisibility();
        }

        private void UnmaskedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;
            Password = UnmaskedTextBox.Text;
            MaskedPasswordBox.Password = Password;
            _isUpdating = false;
            UpdatePlaceholderVisibility();
        }

        private void SyncPasswordFromProperty()
        {
            if (_isUpdating) return;
            _isUpdating = true;
            string pwd = Password ?? string.Empty;
            MaskedPasswordBox.Password = pwd;
            UnmaskedTextBox.Text = pwd;
            _isUpdating = false;
        }

        private void InputControl_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void InputControl_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void UpdatePlaceholderVisibility()
        {
            if (PlaceholderTextBlock == null) return;

            bool hasText = !string.IsNullOrEmpty(Password);
            bool isFocused = MaskedPasswordBox.IsFocused || UnmaskedTextBox.IsFocused;

            PlaceholderTextBlock.Visibility = (hasText || isFocused) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ToggleVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            _showPassword = !_showPassword;

            if (_showPassword)
            {
                EyeIcon.Kind = PackIconKind.EyeOffOutline;
                UnmaskedTextBox.Visibility = Visibility.Visible;
                MaskedPasswordBox.Visibility = Visibility.Collapsed;
                UnmaskedTextBox.Focus();
                if (!string.IsNullOrEmpty(UnmaskedTextBox.Text))
                {
                    UnmaskedTextBox.SelectionStart = UnmaskedTextBox.Text.Length;
                }
            }
            else
            {
                EyeIcon.Kind = PackIconKind.EyeOutline;
                MaskedPasswordBox.Visibility = Visibility.Visible;
                UnmaskedTextBox.Visibility = Visibility.Collapsed;
                MaskedPasswordBox.Focus();
            }
            UpdatePlaceholderVisibility();
        }
    }
}
