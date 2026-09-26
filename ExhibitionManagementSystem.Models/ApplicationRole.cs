using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل دور صلاحية عالميًا ثابتًا في نظام الهوية.
/// </summary>
public class ApplicationRole : IdentityRole, IAuditableEntity
{
    /// <summary>
    /// يحفظ ارتباط الدور القديم أثناء الانتقال إلى الأدوار العالمية.
    /// لا يُستخدم في التفويض أو في إنشاء أدوار جديدة.
    /// </summary>
    [Column("TenantID")]
    public int? LegacyTenantID { get; set; }

    /// <summary>
    /// تاريخ إنشاء الدور.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على بيانات الدور.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
