using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Admin? Admin { get; set; }

    public virtual Staff? Staff { get; set; }

    public virtual Student? Student { get; set; }

    public virtual Teacher? Teacher { get; set; }
}
