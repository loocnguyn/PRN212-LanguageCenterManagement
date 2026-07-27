using BusinessObjects;

namespace DataAccessObjects;

// UserDAO — EF data access for User (CRUD + queries).
//
// Email is the credential, so lookups compare it case-insensitively: the column
// is unique, but "Cam@Mail.com" and "cam@mail.com" are the same person as far as
// signing in is concerned.

public class UserDAO
{
    public static List<User> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Users.ToList();
    }

    public static User? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Users.FirstOrDefault(x => x.Id == id);
    }

    public static User? GetByEmail(string email)
    {
        using var context = new LanguageCenterContext();
        var normalized = email.Trim().ToLower();
        return context.Users.FirstOrDefault(u => u.Email.ToLower() == normalized);
    }

    /// <summary>
    /// Whether the address is already somebody's login. <paramref name="exceptUserId"/>
    /// lets an edit screen ignore the row it is editing.
    /// </summary>
    public static bool IsEmailTaken(string email, int? exceptUserId = null)
    {
        using var context = new LanguageCenterContext();
        var normalized = email.Trim().ToLower();
        return context.Users.Any(u => u.Email.ToLower() == normalized
                                      && (exceptUserId == null || u.Id != exceptUserId));
    }

    public static void Save(User entity)
    {
        using var context = new LanguageCenterContext();
        context.Users.Add(entity);
        context.SaveChanges();
    }

    public static void Update(User entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Users.Find(entity.Id);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Users.Find(id);
        if (existing == null) return;
        existing.IsActive = false;
        context.SaveChanges();
    }

    public static List<User> Search(string keyword)
    {
        using var context = new LanguageCenterContext();
        var kw = keyword.ToLower();
        return context.Users
            .Where(u => u.Email.ToLower().Contains(kw) || u.Role.ToLower().Contains(kw))
            .ToList();
    }
}
