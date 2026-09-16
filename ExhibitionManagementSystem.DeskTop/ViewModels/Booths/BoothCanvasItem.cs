using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Booths;

public partial class BoothCanvasItem : ObservableObject
{
    public int BoothID { get; set; }
    public string BoothNumber { get; set; } = string.Empty;
    
    [ObservableProperty]
    private string _status = string.Empty; // Available | Reserved | PendingReview

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _width = 80;

    [ObservableProperty]
    private double _height = 60;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _hasCollision;

    [ObservableProperty]
    private string _shapeType = "Rectangle";

    [ObservableProperty]
    private double _rotationAngle;

    [ObservableProperty]
    private ObservableCollection<FurnitureItem> _furniture = new();
}

public partial class FurnitureItem : ObservableObject
{
    [ObservableProperty] private string _type = "Chair";
    [ObservableProperty] private double _x = 10;
    [ObservableProperty] private double _y = 10;
    [ObservableProperty] private double _width = 24;
    [ObservableProperty] private double _height = 24;
    [ObservableProperty] private double _rotationAngle = 0;
    [ObservableProperty] private bool _isSelected;
}
