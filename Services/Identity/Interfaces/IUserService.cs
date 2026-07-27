using BusinessObjects;

namespace Services;

// IUserService — service contract for User operations.

public interface IUserService
{
    List<User> GetAll();
    User? GetById(int id);

    /// <summary>The account whose login is this address, or null. Case-insensitive.</summary>
    User? GetByEmail(string email);

    /// <summary>Whether the address is already a login; pass the edited row's id to ignore itself.</summary>
    bool IsEmailTaken(string email, int? exceptUserId = null);

    /// <summary>
    /// user id -> the person's name, gathered from whichever profile table holds it.
    /// The account screens list Users, but a User only carries an email; this is how
    /// they show who the account actually belongs to without a query per row.
    /// </summary>
    Dictionary<int, string> GetDisplayNames();

    /// <summary>
    /// Creates an account. <paramref name="mustChangePassword"/> defaults to true because
    /// the caller is almost always an admin or an import typing a password on somebody
    /// else's behalf — see <see cref="User.MustChangePassword"/>.
    /// </summary>
    void Save(User user, string plainPassword, bool mustChangePassword = true);

    void Update(User user);

    /// <summary>Sets a new password and clears the must-change flag.</summary>
    void UpdatePassword(int id, string newPlainPassword);

    void Delete(int id);

    /// <summary>
    /// Signs in with the account's email. Returns null when the address is unknown,
    /// the password is wrong, or the account is deactivated — the caller must not say
    /// which, or the login screen turns into a way to test whether an address exists.
    /// </summary>
    User? Login(string email, string password);
}
