namespace ExhibitionManagementSystem.DataAccess.Repositories;

public static class InvoiceNumberFormatter
{
    public static string Format(int tenantId, int year, int sequence)
    {
        if (tenantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tenantId));
        }

        if (year is < 2000 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        return $"INV-{tenantId:D4}-{year:D4}-{sequence:D5}";
    }
}
