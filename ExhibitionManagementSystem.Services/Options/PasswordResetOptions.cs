namespace ExhibitionManagementSystem.Services.Options;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";
    public string PublicAppUrl { get; set; } = string.Empty;
}
