using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل دور صلاحية في نظام الهوية مرتبطًا بمستأجر محدد.
/// </summary>
public class ApplicationRole : IdentityRole, IAuditableEntity
{
    /// <summary>
    /// معرف المستأجر الذي يملك هذا الدور.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// المستأجر المرتبط بالدور.
    /// </summary>
    public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// تاريخ إنشاء الدور.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على بيانات الدور.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
