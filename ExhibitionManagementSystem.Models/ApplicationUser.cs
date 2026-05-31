using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;


namespace ExhibitionManagementSystem.Models;

public class ApplicationUser:IdentityUser
{

    public int TenantID { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public virtual Tenant Tenant { get; set; }



}
