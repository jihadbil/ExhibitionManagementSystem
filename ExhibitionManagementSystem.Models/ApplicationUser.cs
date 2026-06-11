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
    public string FullName { get; set; }

    /// <summary>
    /// يحدد ما إذا كان حساب المستخدم مفعّلًا.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// تاريخ ووقت آخر تسجيل دخول ناجح للمستخدم.
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// رمز التحديث المستخدم لإصدار رموز وصول جديدة.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// تاريخ انتهاء صلاحية رمز التحديث.
    /// </summary>
    public DateTime? RefreshTokenExpiry { get; set; }

    /// <summary>
    /// المستأجر المرتبط بالمستخدم.
    /// </summary>
    public virtual Tenant Tenant { get; set; }



}
