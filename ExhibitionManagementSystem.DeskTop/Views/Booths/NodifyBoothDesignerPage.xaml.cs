using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ExhibitionManagementSystem.DeskTop.ViewModels.Booths;

namespace ExhibitionManagementSystem.DeskTop.Views.Booths;

public partial class NodifyBoothDesignerPage : UserControl
{
    public NodifyBoothDesignerViewModel ViewModel { get; }

    public NodifyBoothDesignerPage(NodifyBoothDesignerViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Focusable = true;
        PreviewKeyDown += Page_PreviewKeyDown;

        Loaded += async (s, e) =>
        {
            await ViewModel.OnNavigatedToAsync();
            Focus();
        };
    }

    private void UpdateSelectionState()
    {
        int selectedCount = 0;
        BoothNodeViewModel? singleSelected = null;

        foreach (var node in ViewModel.BoothNodes)
        {
            if (node.IsSelected)
            {
                selectedCount++;
                singleSelected = node;
            }
        }

        ViewModel.IsMultiSelected = selectedCount > 1;
        
        if (selectedCount == 1)
        {
            ViewModel.SelectedNode = singleSelected;
        }
        else if (selectedCount == 0)
        {
            ViewModel.SelectedNode = null;
        }
    }

    private async void BoothEditor_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Update multi-selection state when mouse button is released
        UpdateSelectionState();

        // Whenever dragging ends, if a node is selected, let's update and save its position
        if (ViewModel.SelectedNode != null)
        {
            // Snap to grid of 20 pixels
            double gridSize = 20.0;
            double snappedX = Math.Round(ViewModel.SelectedNode.Location.X / gridSize) * gridSize;
            double snappedY = Math.Round(ViewModel.SelectedNode.Location.Y / gridSize) * gridSize;

            // Magnet Snapping: Snap to other nodes' alignment edges if within 12 pixels
            foreach (var other in ViewModel.BoothNodes)
            {
                if (other == ViewModel.SelectedNode) continue;
                
                // Snap Left to other Left
                if (Math.Abs(ViewModel.SelectedNode.Location.X - other.Location.X) < 12.0)
                    snappedX = other.Location.X;
                // Snap Right to other Right
                else if (Math.Abs((ViewModel.SelectedNode.Location.X + ViewModel.SelectedNode.Width) - (other.Location.X + other.Width)) < 12.0)
                    snappedX = other.Location.X + other.Width - ViewModel.SelectedNode.Width;
                
                // Snap Top to other Top
                if (Math.Abs(ViewModel.SelectedNode.Location.Y - other.Location.Y) < 12.0)
                    snappedY = other.Location.Y;
                // Snap Bottom to other Bottom
                else if (Math.Abs((ViewModel.SelectedNode.Location.Y + ViewModel.SelectedNode.Height) - (other.Location.Y + other.Height)) < 12.0)
                    snappedY = other.Location.Y + other.Height - ViewModel.SelectedNode.Height;
            }

            ViewModel.SelectedNode.Location = new Point(snappedX, snappedY);

            // Check for collision
            if (ViewModel.CheckForCollisionsAndReturnTrueIfAny())
            {
                ViewModel.Notifications.ShowWarning("⚠️ تداخل غير مسموح به مع جناح آخر! تم إلغاء الحركة.");
                await ViewModel.UndoCommand.ExecuteAsync(null);
                return;
            }

            await ViewModel.SaveBoothPositionAsync(ViewModel.SelectedNode);
        }
    }

    private async void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Keyboard Shortcuts for Undo / Redo
        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (e.Key == Key.Z)
            {
                e.Handled = true;
                await ViewModel.UndoCommand.ExecuteAsync(null);
                return;
            }
            if (e.Key == Key.Y)
            {
                e.Handled = true;
                await ViewModel.RedoCommand.ExecuteAsync(null);
                return;
            }
        }

        if (ViewModel.SelectedNode == null) return;
        if (e.OriginalSource is TextBox) return; // Prevent nudging when typing in textboxes

        // 2 pixels step (10cm) or 20 pixels step (1m) when holding Shift
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
            ViewModel.SelectedNode.Location = new Point(
                ViewModel.SelectedNode.Location.X + deltaX,
                ViewModel.SelectedNode.Location.Y + deltaY
            );
            ViewModel.CheckForCollisions();
            await ViewModel.SaveBoothPositionAsync(ViewModel.SelectedNode);
        }
    }

    // ━━━━━━━━━━━━━━ Direct Drag Resizing & Rotation handlers ━━━━━━━━━━━━━━

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is Thumb thumb && thumb.DataContext is BoothNodeViewModel node)
        {
            // Limit minimum size to 1.5m (30px)
            node.Width = Math.Max(30, node.Width + e.HorizontalChange);
            node.Height = Math.Max(30, node.Height + e.VerticalChange);

            if (ViewModel.SelectedNode == node)
            {
                ViewModel.EditWidth = node.Width / BoothNodeViewModel.MeterToPixel;
                ViewModel.EditHeight = node.Height / BoothNodeViewModel.MeterToPixel;
            }
        }
    }

    private async void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (sender is Thumb thumb && thumb.DataContext is BoothNodeViewModel node)
        {
            // Snap width/height to grid increments of 10 pixels (0.5 meter)
            double snapStep = 10.0;
            node.Width = Math.Round(node.Width / snapStep) * snapStep;
            node.Height = Math.Round(node.Height / snapStep) * snapStep;

            if (ViewModel.SelectedNode == node)
            {
                ViewModel.EditWidth = node.Width / BoothNodeViewModel.MeterToPixel;
                ViewModel.EditHeight = node.Height / BoothNodeViewModel.MeterToPixel;
            }

            await ViewModel.SaveBoothPositionAsync(node);
        }
    }

    private void RotateThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is Thumb thumb && thumb.DataContext is BoothNodeViewModel node)
        {
            // Find current mouse position relative to the editor
            Point currentPos = Mouse.GetPosition(BoothEditor);
            
            // Calculate center of node
            Point center = new Point(node.Location.X + node.Width / 2, node.Location.Y + node.Height / 2);
            
            // Calculate angle
            double angleRad = Math.Atan2(currentPos.Y - center.Y, currentPos.X - center.X);
            double angleDeg = angleRad * (180.0 / Math.PI);
            
            // Adjust offset
            angleDeg += 90;
            
            if (angleDeg < 0) angleDeg += 360;
            
            // Snap rotation to increments of 5 degrees during active drag for precision
            double snapAngle = 5.0;
            node.RotationAngle = Math.Round(angleDeg / snapAngle) * snapAngle;

            if (ViewModel.SelectedNode == node)
            {
                ViewModel.EditRotation = node.RotationAngle;
            }
        }
    }

    private async void RotateThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (sender is Thumb thumb && thumb.DataContext is BoothNodeViewModel node)
        {
            await ViewModel.SaveBoothPositionAsync(node);
        }
    }

    private void BoothEditor_MouseMove(object sender, MouseEventArgs e)
    {
        // Calculate mouse position relative to the main editor grid
        Point pos = e.GetPosition(BoothEditor);
        double xMeters = pos.X / BoothNodeViewModel.MeterToPixel;
        double yMeters = pos.Y / BoothNodeViewModel.MeterToPixel;
        ViewModel.CursorCoordinateString = $"{xMeters:0.0}م ، {yMeters:0.0}م";
    }

    private void Node_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Save state before drag starts
        ViewModel.SaveHistoryState();
    }

    private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        // Save state before resizing starts
        ViewModel.SaveHistoryState();
    }

    private void RotateThumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        // Save state before rotation starts
        ViewModel.SaveHistoryState();
    }
}
