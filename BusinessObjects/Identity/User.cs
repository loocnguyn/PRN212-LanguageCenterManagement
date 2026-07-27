using System;
using System.Collections.Generic;

namespace BusinessObjects;

// User — domain model.
//
// Email IS the credential — there is no username. The profile tables carry their
// own contact email; this one is what the sign-in screen matches against and the
// only one the database keeps unique.

public partial class User
{
    public int Id { get; set; }

    /// <summary>Sign-in identity. Unique across all accounts, stored lowercase.</summary>
    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    /// <summary>
    /// True while the account still has the password somebody else set for it
    /// (admin-created or CSV-imported). Login succeeds but the app stays locked
    /// behind the change-password prompt until it is cleared.
    /// </summary>
    public bool MustChangePassword { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Admin? Admin { get; set; }

    public virtual Staff? Staff { get; set; }

    public virtual Student? Student { get; set; }

    public virtual Teacher? Teacher { get; set; }
}
