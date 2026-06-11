using System;

namespace ExhibitionManagementSystem.Models.Interfaces;

/// <summary>
/// يحدد الخصائص الأساسية لتتبع تاريخ إنشاء وتعديل الكيان.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// تاريخ إنشاء الكيان.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// تاريخ آخر تعديل على الكيان.
    /// </summary>
    DateTime? UpdatedAt { get; set; }
}
