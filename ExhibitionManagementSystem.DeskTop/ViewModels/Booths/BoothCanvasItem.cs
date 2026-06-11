using CommunityToolkit.Mvvm.ComponentModel;

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
}
