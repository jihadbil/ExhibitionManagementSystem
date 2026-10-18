namespace ExhibitionManagementSystem.Services.Common;

public static class EnumInput
{
    public static bool TryParse<T>(string? value, out T parsed) where T : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);
}
