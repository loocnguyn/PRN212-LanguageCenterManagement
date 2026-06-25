using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class Admin
{
    public int AdminId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public virtual User User { get; set; } = null!;
}
