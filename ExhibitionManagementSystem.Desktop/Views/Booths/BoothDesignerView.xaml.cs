using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExhibitionManagementSystem.Desktop.ViewModels.Booths;
using ExhibitionManagementSystem.Models.DTOs.Booth;

namespace ExhibitionManagementSystem.Desktop.Views.Booths
{
    public partial class BoothDesignerView : UserControl
    {
        private bool _isDragging;
        private Point _clickPosition;
        private BoothDto? _draggedItem;
        private double _originalItemX;
        private double _originalItemY;

        public BoothDesignerView()
        {
            InitializeComponent();
        }

        private void Booth_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is BoothDto item)
            {
                if (DataContext is BoothDesignerViewModel vm)
                {
                    vm.SelectedBooth = item;
                }

                _draggedItem = item;
                _clickPosition = e.GetPosition(this); // click position relative to view
                _originalItemX = (double)(item.PosX ?? 0);
                _originalItemY = (double)(item.PosY ?? 0);
                _isDragging = true;
                
                element.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Booth_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedItem != null && sender is FrameworkElement element)
            {
                Point currentPos = e.GetPosition(this);
                double deltaX = currentPos.X - _clickPosition.X;
                double deltaY = currentPos.Y - _clickPosition.Y;

                // Adjust movement delta by canvas zoom scale to keep movements matched
                if (DataContext is BoothDesignerViewModel vm)
                {
                    deltaX /= vm.CanvasScale;
                    deltaY /= vm.CanvasScale;
                }

                // Grid snapping of 10 pixels for cleaner dragging layouts
                double newX = _originalItemX + deltaX;
                double newY = _originalItemY + deltaY;

                // Simple boundaries checks
                if (newX < 0) newX = 0;
                if (newY < 0) newY = 0;

                _draggedItem.PosX = (decimal)(Math.Round(newX / 10.0) * 10.0);
                _draggedItem.PosY = (decimal)(Math.Round(newY / 10.0) * 10.0);
                e.Handled = true;
            }
        }

        private void Booth_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && sender is FrameworkElement element)
            {
                element.ReleaseMouseCapture();
                _isDragging = false;
                _draggedItem = null;
                e.Handled = true;
            }
        }
    }
}
