using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExhibitionManagementSystem.Models;

public class ApplicationRole:IdentityRole
{

    public int TenantID { get; set; }
    public virtual Tenant Tenant { get; set; }
}
