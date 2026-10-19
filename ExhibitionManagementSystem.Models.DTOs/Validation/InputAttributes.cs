using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DecimalValueAttribute(string minimum, string maximum, int scale = 2) : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null || value is decimal number
        && number >= decimal.Parse(minimum, System.Globalization.CultureInfo.InvariantCulture)
        && number <= decimal.Parse(maximum, System.Globalization.CultureInfo.InvariantCulture)
        && decimal.Round(number, scale) == number;

    public override string FormatErrorMessage(string name) => $"{name} must be between {minimum} and {maximum}, with at most {scale} decimal places.";
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class DateAfterAttribute(string startProperty) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var start = context.ObjectType.GetProperty(startProperty)!.GetValue(context.ObjectInstance);
        return value is null || (value is DateTime end && start is DateTime begin && begin != default && end >= begin)
            ? ValidationResult.Success : new ValidationResult($"{context.MemberName} must be on or after {startProperty}.", [context.MemberName!]);
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class DefinedEnumAttribute(Type enumType) : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null || value is string text
        && (string.IsNullOrEmpty(text) || Enum.TryParse(enumType, text, true, out var parsed) && Enum.IsDefined(enumType, parsed!))
        || value is not string && Enum.IsDefined(enumType, value!);
}

public static class EnumValues
{
    public static T Parse<T>(string value, bool ignoreCase = true) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase, out var result) && Enum.IsDefined(result)
            ? result : throw new ValidationException($"Invalid {typeof(T).Name} value.");
}
