using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;


namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل مستخدم التطبيق مع بيانات الهوية والارتباط بالمستأجر.
/// </summary>
public class ApplicationUser:IdentityUser
{

    /// <summary>
    /// معرف المستأجر الذي ينتمي إليه المستخدم.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// الاسم الكامل للمستخدم.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// يحدد ما إذا كان حساب المستخدم مفعّلًا.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// تاريخ ووقت آخر تسجيل دخول ناجح للمستخدم.
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// المستأجر المرتبط بالمستخدم.
    /// </summary>
    public virtual Tenant Tenant { get; set; } = null!;

    public virtual ICollection<RefreshSession> RefreshSessions { get; set; } = new HashSet<RefreshSession>();



}
