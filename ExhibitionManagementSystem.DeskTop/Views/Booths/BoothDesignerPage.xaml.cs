using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExhibitionManagementSystem.DeskTop.ViewModels.Booths;

namespace ExhibitionManagementSystem.DeskTop.Views.Booths;

public partial class BoothDesignerPage : UserControl
{
    public BoothDesignerViewModel ViewModel { get; }

    private bool _isDragging;
    private Point _clickPosition;
    private double _originX;
    private double _originY;

    public BoothDesignerPage(BoothDesignerViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }

    private void Booth_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var element = (FrameworkElement)sender;
        if (element.DataContext is BoothCanvasItem item)
        {
            // Set selection in VM
            ViewModel.SelectedBooth = item;

            element.CaptureMouse();
            _isDragging = true;
            _clickPosition = e.GetPosition(DesignerCanvas);
            _originX = item.X;
            _originY = item.Y;

            e.Handled = true;
        }
    }

    private void Booth_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var element = (FrameworkElement)sender;
            if (element.DataContext is BoothCanvasItem item)
            {
                var currentPosition = e.GetPosition(DesignerCanvas);

                double deltaX = currentPosition.X - _clickPosition.X;
                double deltaY = currentPosition.Y - _clickPosition.Y;

                item.X = _originX + deltaX;
                item.Y = _originY + deltaY;
            }
        }
    }

    private async void Booth_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            var element = (FrameworkElement)sender;
            element.ReleaseMouseCapture();
            _isDragging = false;

            if (element.DataContext is BoothCanvasItem item)
            {
                // Auto-save on release
                await ViewModel.SaveBoothPositionAsync(item);
            }
        }
    }
}
