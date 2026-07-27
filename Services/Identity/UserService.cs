using BusinessObjects;
using Repositories;

namespace Services;

// ============================================================
//  UserService — accounts and sign-in.
//
//  The account's EMAIL is its credential. Addresses are normalised to lowercase
//  on the way in so the stored value and every later comparison agree; the
//  database's UNIQUE index is the last line of defence, but IsEmailTaken is what
//  turns a collision into a readable message instead of a SQL exception.
// ============================================================

public class UserService : IUserService
{
    private readonly IUserRepository _repo = new UserRepository();

    public List<User> GetAll() => _repo.GetAll();

    public User? GetById(int id) => _repo.GetById(id);

    public User? GetByEmail(string email) => _repo.GetByEmail(email);

    public bool IsEmailTaken(string email, int? exceptUserId = null)
        => _repo.IsEmailTaken(email, exceptUserId);

    public void Update(User user)
    {
        user.Email = Normalize(user.Email);
        _repo.Update(user);
    }

    public void Delete(int id) => _repo.Delete(id);

    public void Save(User user, string plainPassword, bool mustChangePassword = true)
    {
        user.Email = Normalize(user.Email);

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidOperationException("An account needs an email address to sign in with.");

        if (_repo.IsEmailTaken(user.Email))
            throw new InvalidOperationException($"{user.Email} is already used by another account.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        user.MustChangePassword = mustChangePassword;
        _repo.Save(user);
    }

    /// <summary>
    /// Sets a new password. This also clears MustChangePassword: whoever just typed
    /// this password chose it themselves, which is the whole point of the flag.
    /// </summary>
    public void UpdatePassword(int id, string newPlainPassword)
    {
        var user = _repo.GetById(id);
        if (user == null) return;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPlainPassword);
        user.MustChangePassword = false;
        _repo.Update(user);
    }

    public User? Login(string email, string password)
    {
        var user = _repo.GetByEmail(Normalize(email));
        if (user == null || !user.IsActive) return null;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    private static string Normalize(string? email) => (email ?? "").Trim().ToLowerInvariant();
}
