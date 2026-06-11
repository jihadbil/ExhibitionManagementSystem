using System;

namespace ExhibitionManagementSystem.Models.Interfaces;

/// <summary>
/// يحدد الخصائص اللازمة لدعم الحذف الناعم دون إزالة السجل فعليًا من قاعدة البيانات.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// يحدد ما إذا كان الكيان محذوفًا حذفًا ناعمًا.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// تاريخ تنفيذ الحذف الناعم.
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// معرف المستخدم الذي نفذ الحذف الناعم.
    /// </summary>
    string? DeletedByUserId { get; set; }
}
