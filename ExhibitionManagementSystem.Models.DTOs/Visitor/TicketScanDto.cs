using System;

namespace ExhibitionManagementSystem.Models.DTOs.Visitor;

public class TicketScanDto
{
    public int ScanID { get; set; }
    public int TicketID { get; set; }
    public string QRCode { get; set; } = string.Empty;
    public DateTime ScanTime { get; set; }
    public string ScanDirection { get; set; } = string.Empty;
    public string ScanLocation { get; set; } = string.Empty;
    public string ScannedByUserId { get; set; } = string.Empty;
}
