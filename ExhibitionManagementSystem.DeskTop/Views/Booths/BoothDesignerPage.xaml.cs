using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using ExhibitionManagementSystem.DeskTop.ViewModels.Booths;

namespace ExhibitionManagementSystem.DeskTop.Views.Booths;

public partial class BoothDesignerPage : UserControl
{
    public BoothDesignerViewModel ViewModel { get; }

    private bool _isDragging;
    private Point _clickPosition;
    private double _originX;
    private double _originY;

    // Pan variables
    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;

    public BoothDesignerPage(BoothDesignerViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Focusable = true;
        PreviewKeyDown += BoothDesignerPage_PreviewKeyDown;

        Loaded += async (s, e) =>
        {
            await ViewModel.OnNavigatedToAsync();
            Focus();
        };
    }

    private void Booth_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var element = (FrameworkElement)sender;
        if (element.DataContext is BoothCanvasItem item)
        {
            // Save state before drag starts
            ViewModel.SaveHistoryState();

            // Set selection in VM
            ViewModel.SelectedBooth = item;

            Focus(); // Focus the UserControl to capture keyboard events
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

                double newX = _originX + deltaX;
                double newY = _originY + deltaY;

                if (ViewModel.IsSnapToGridEnabled)
                {
                    newX = Math.Round(newX / ViewModel.GridSnapSize) * ViewModel.GridSnapSize;
                    newY = Math.Round(newY / ViewModel.GridSnapSize) * ViewModel.GridSnapSize;
                }

                item.X = newX;
                item.Y = newY;

                ViewModel.CheckForCollisions();
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

    private void BoothDesignerPage_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Handle Ctrl+Z and Ctrl+Y shortcuts (works even with no selection)
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            if (e.Key == Key.Z)
            {
                e.Handled = true;
                _ = ViewModel.UndoCommand.ExecuteAsync(null);
                return;
            }
            if (e.Key == Key.Y)
            {
                e.Handled = true;
                _ = ViewModel.RedoCommand.ExecuteAsync(null);
                return;
            }
        }

        if (ViewModel.SelectedBooth is null) return;
        if (e.OriginalSource is TextBox) return; // Prevent nudging when typing in textboxes

        double step = e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift) ? 20 : 2;
        bool moved = false;

        double deltaX = 0;
        double deltaY = 0;

        switch (e.Key)
        {
            case Key.Left:
                deltaX = -step;
                moved = true;
                break;
            case Key.Right:
                deltaX = step;
                moved = true;
                break;
            case Key.Up:
                deltaY = -step;
                moved = true;
                break;
            case Key.Down:
                deltaY = step;
                moved = true;
                break;
        }

        if (moved)
        {
            e.Handled = true;

            // Save history state before moving
            ViewModel.SaveHistoryState();

            ViewModel.SelectedBooth.X += deltaX;
            ViewModel.SelectedBooth.Y += deltaY;

            if (ViewModel.IsSnapToGridEnabled)
            {
                ViewModel.SelectedBooth.X = Math.Round(ViewModel.SelectedBooth.X / ViewModel.GridSnapSize) * ViewModel.GridSnapSize;
                ViewModel.SelectedBooth.Y = Math.Round(ViewModel.SelectedBooth.Y / ViewModel.GridSnapSize) * ViewModel.GridSnapSize;
            }

            ViewModel.CheckForCollisions();

            // Auto-save position change asynchronously
            _ = ViewModel.SaveBoothPositionAsync(ViewModel.SelectedBooth);
        }
    }

    // ━━━━━━━━━━━━━━ ScrollViewer Pan & Zoom Event Handlers ━━━━━━━━━━━━━━

    private void CanvasScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle || (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.Space)))
        {
            var scrollViewer = (ScrollViewer)sender;
            _isPanning = true;
            _panStartPoint = e.GetPosition(scrollViewer);
            _panStartHorizontalOffset = scrollViewer.HorizontalOffset;
            _panStartVerticalOffset = scrollViewer.VerticalOffset;
            scrollViewer.CaptureMouse();
            e.Handled = true;
        }
    }

    private void CanvasScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            var scrollViewer = (ScrollViewer)sender;
            Point currentPoint = e.GetPosition(scrollViewer);
            double deltaX = currentPoint.X - _panStartPoint.X;
            double deltaY = currentPoint.Y - _panStartPoint.Y;

            scrollViewer.ScrollToHorizontalOffset(_panStartHorizontalOffset - deltaX);
            scrollViewer.ScrollToVerticalOffset(_panStartVerticalOffset - deltaY);
            e.Handled = true;
        }
    }

    private void CanvasScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning && (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Left))
        {
            var scrollViewer = (ScrollViewer)sender;
            scrollViewer.ReleaseMouseCapture();
            _isPanning = false;
            e.Handled = true;
        }
    }

    private void CanvasScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            var scrollViewer = (ScrollViewer)sender;
            var zoomContent = (FrameworkElement)scrollViewer.Content;
            Point relativeMousePos = e.GetPosition(zoomContent);

            double oldScale = ViewModel.ScaleFactor;
            double zoomDelta = e.Delta > 0 ? 0.1 : -0.1;
            double newScale = Math.Clamp(oldScale + zoomDelta, 0.5, 3.0);

            if (Math.Abs(newScale - oldScale) > 0.001)
            {
                ViewModel.ScaleFactor = newScale;
                zoomContent.UpdateLayout();

                double mouseXOnCanvas = relativeMousePos.X / oldScale;
                double mouseYOnCanvas = relativeMousePos.Y / oldScale;

                double newMouseX = mouseXOnCanvas * newScale;
                double newMouseY = mouseYOnCanvas * newScale;

                Point mousePosInViewer = e.GetPosition(scrollViewer);

                double newHorizontalOffset = newMouseX - mousePosInViewer.X;
                double newVerticalOffset = newMouseY - mousePosInViewer.Y;

                scrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
                scrollViewer.ScrollToVerticalOffset(newVerticalOffset);
            }
            e.Handled = true;
        }
    }

    // ━━━━━━━━━━━━━━ Direct Canvas Interaction Resizing & Rotating ━━━━━━━━━━━━━━

    private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        ViewModel.SaveHistoryState();
    }

    private async void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        var thumb = (Thumb)sender;
        if (thumb.DataContext is BoothCanvasItem item)
        {
            await ViewModel.SaveBoothPositionAsync(item);
        }
    }

    private async void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var thumb = (Thumb)sender;
        var item = (BoothCanvasItem)thumb.DataContext;
        if (item == null) return;

        double horizontalChange = e.HorizontalChange;
        double verticalChange = e.VerticalChange;
        string direction = thumb.Tag.ToString() ?? string.Empty;

        // Min booth size is 1 meter (20 pixels)
        double minSize = 20;

        switch (direction)
        {
            case "TopLeft":
                if (item.Width - horizontalChange > minSize)
                {
                    item.X += horizontalChange;
                    item.Width -= horizontalChange;
                }
                if (item.Height - verticalChange > minSize)
                {
                    item.Y += verticalChange;
                    item.Height -= verticalChange;
                }
                break;

            case "TopRight":
                if (item.Width + horizontalChange > minSize)
                {
                    item.Width += horizontalChange;
                }
                if (item.Height - verticalChange > minSize)
                {
                    item.Y += verticalChange;
                    item.Height -= verticalChange;
                }
                break;

            case "BottomLeft":
                if (item.Width - horizontalChange > minSize)
                {
                    item.X += horizontalChange;
                    item.Width -= horizontalChange;
                }
                if (item.Height + verticalChange > minSize)
                {
                    item.Height += verticalChange;
                }
                break;

            case "BottomRight":
                if (item.Width + horizontalChange > minSize)
                {
                    item.Width += horizontalChange;
                }
                if (item.Height + verticalChange > minSize)
                {
                    item.Height += verticalChange;
                }
                break;
        }

        // Apply Snap to Grid to resizing
        if (ViewModel.IsSnapToGridEnabled)
        {
            item.Width = Math.Round(item.Width / ViewModel.GridSnapSize) * ViewModel.GridSnapSize;
            item.Height = Math.Round(item.Height / ViewModel.GridSnapSize) * ViewModel.GridSnapSize;
            item.X = Math.Round(item.X / ViewModel.GridSnapSize) * ViewModel.GridSnapSize;
            item.Y = Math.Round(item.Y / ViewModel.GridSnapSize) * ViewModel.GridSnapSize;
        }

        ViewModel.CheckForCollisions();
    }

    private void RotateThumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        ViewModel.SaveHistoryState();
    }

    private async void RotateThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        var thumb = (Thumb)sender;
        if (thumb.DataContext is BoothCanvasItem item)
        {
            await ViewModel.SaveBoothPositionAsync(item);
        }
    }

    private void RotateThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var thumb = (Thumb)sender;
        if (thumb.DataContext is BoothCanvasItem item)
        {
            Point currentPoint = Mouse.GetPosition(DesignerCanvas);

            double centerX = item.X + item.Width / 2;
            double centerY = item.Y + item.Height / 2;

            double deltaX = currentPoint.X - centerX;
            double deltaY = currentPoint.Y - centerY;

            // Calculate angle in degrees
            double angle = Math.Atan2(deltaY, deltaX) * (180 / Math.PI);

            // Add 90 degrees since rotation handle is at 12 o'clock (top)
            angle += 90;

            if (angle < 0) angle += 360;

            // Snap to nearest 1 degree
            angle = Math.Round(angle);

            item.RotationAngle = angle;
            ViewModel.SelectedBoothRotation = angle; // Sync with slider
            ViewModel.CheckForCollisions();
        }
    }

    // ━━━━━━━━━━━━━━ Furniture Direct Dragging & Selection ━━━━━━━━━━━━━━

    private bool _isDraggingFurniture;
    private FurnitureItem? _draggedFurniture;
    private Point _furnitureDragStartPoint;

    private void Furniture_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var element = (FrameworkElement)sender;
        var furniture = (FurnitureItem)element.DataContext;
        if (furniture == null) return;

        // Select the furniture item
        ViewModel.SelectedFurniture = furniture;
        
        // Mark as selected visually
        var selectedBooth = ViewModel.SelectedBooth;
        if (selectedBooth != null)
        {
            foreach (var f in selectedBooth.Furniture)
            {
                f.IsSelected = (f == furniture);
            }
        }

        _isDraggingFurniture = true;
        _draggedFurniture = furniture;
        _furnitureDragStartPoint = e.GetPosition(element);

        element.CaptureMouse();
        
        // Save history state on drag start
        ViewModel.SaveHistoryState();

        // Mark event as handled so the booth dragging does not fire!
        e.Handled = true;
    }

    private void Furniture_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingFurniture && _draggedFurniture != null)
        {
            var element = (FrameworkElement)sender;
            
            // Find parent Grid (the immediate parent inside the booth ItemTemplate)
            // It holds the furniture ItemsControl
            var parentGrid = (FrameworkElement)element.Parent;
            if (parentGrid == null) return;

            Point mousePos = e.GetPosition(parentGrid);

            // Calculate coordinates relative to the booth's top-left
            double newX = mousePos.X - _furnitureDragStartPoint.X;
            double newY = mousePos.Y - _furnitureDragStartPoint.Y;

            // Restrict coordinates inside booth boundaries
            var selectedBooth = ViewModel.SelectedBooth;
            if (selectedBooth != null)
            {
                newX = Math.Clamp(newX, 0, selectedBooth.Width - _draggedFurniture.Width);
                newY = Math.Clamp(newY, 0, selectedBooth.Height - _draggedFurniture.Height);
            }

            _draggedFurniture.X = newX;
            _draggedFurniture.Y = newY;

            e.Handled = true;
        }
    }

    private async void Furniture_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingFurniture)
        {
            var element = (FrameworkElement)sender;
            element.ReleaseMouseCapture();
            _isDraggingFurniture = false;
            _draggedFurniture = null;

            if (ViewModel.SelectedBooth != null)
            {
                await ViewModel.SaveBoothPositionAsync(ViewModel.SelectedBooth);
            }

            e.Handled = true;
        }
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Deselect currently selected furniture
        if (ViewModel.SelectedBooth != null)
        {
            foreach (var f in ViewModel.SelectedBooth.Furniture)
            {
                f.IsSelected = false;
            }
        }
        ViewModel.SelectedFurniture = null;
        ViewModel.SelectedBooth = null;
    }
}
