using BusinessObjects;

namespace DataAccessObjects;

// AdminDAO — EF data access for Admin (CRUD + queries).

public class AdminDAO
{
    public static List<Admin> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Admins.ToList();
    }

    public static Admin? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Admins.FirstOrDefault(x => x.AdminId == id);
    }

    /// <summary>The admin profile belonging to a user account, or null.</summary>
    public static Admin? GetByUserId(int userId)
    {
        using var context = new LanguageCenterContext();
        return context.Admins.FirstOrDefault(x => x.UserId == userId);
    }

    public static void Save(Admin entity)
    {
        using var context = new LanguageCenterContext();
        context.Admins.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Admin entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Admins.Find(entity.AdminId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Admins.Find(id);
        if (existing == null) return;
        context.Admins.Remove(existing);
        context.SaveChanges();
    }
}
