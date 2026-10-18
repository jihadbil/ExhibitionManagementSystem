namespace ExhibitionManagementSystem.Services.Common;

internal static class MoneyInput
{
    // Source prices, payments and credit amounts are persisted as decimal(18,2).
    public static bool IsValid(decimal amount, bool allowZero = false) => (allowZero ? amount >= 0 : amount > 0) &&
        amount <= 9999999999999999.99m && decimal.Round(amount, 2) == amount;
}
