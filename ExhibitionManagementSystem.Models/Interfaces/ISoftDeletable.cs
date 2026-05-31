using System;

namespace ExhibitionManagementSystem.Models.Interfaces;

/// <summary>
/// يُمكّن الحذف الناعم (Soft Delete) على الكيان.
/// السجلات المحذوفة لا تُزال فعلياً بل تُوسم بـ IsDeleted.
/// يجب تطبيق Global Query Filter في ApplicationDbContext.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedByUserId { get; set; }
}
