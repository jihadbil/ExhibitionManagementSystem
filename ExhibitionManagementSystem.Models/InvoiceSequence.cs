using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// عداد داخلي ذري لأرقام الفواتير لكل مستأجر وسنة.
/// </summary>
public sealed class InvoiceSequence
{
    public int TenantID { get; set; }
    public int Year { get; set; }
    public int LastValue { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
