using System;

namespace ExhibitionManagementSystem.Models.Interfaces;

/// <summary>
/// يضمن أن الكيان يحتفظ بتواريخ الإنشاء والتعديل.
/// يُطبَّق على جميع النماذج الرئيسية.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
