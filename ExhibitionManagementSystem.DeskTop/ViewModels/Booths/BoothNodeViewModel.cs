using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ExhibitionManagementSystem.DeskTop.ViewModels.Booths;

public partial class BoothNodeViewModel : ObservableObject
{
    public int BoothID { get; set; }

    [ObservableProperty]
    private Point _location;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WidthMeters))]
    private double _width = 80;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeightMeters))]
    private double _height = 60;

    [ObservableProperty]
    private string _boothNumber = string.Empty;

    [ObservableProperty]
    private string _status = "Available"; // Available | Reserved | PendingReview

    [ObservableProperty]
    private string _shapeType = "Rectangle";

    [ObservableProperty]
    private double _rotationAngle;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _hasCollision;

    [ObservableProperty]
    private bool _isVIP;

    [ObservableProperty]
    private string _shapePolygonJSON = string.Empty;

    public const double MeterToPixel = 20.0;

    public double WidthMeters => Width / MeterToPixel;
    public double HeightMeters => Height / MeterToPixel;
}
