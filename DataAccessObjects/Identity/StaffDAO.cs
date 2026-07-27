using BusinessObjects;

namespace DataAccessObjects;

// StaffDAO — EF data access for Staff (CRUD + queries).

public class StaffDAO
{
    public static List<Staff> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Staff.ToList();
    }

    public static Staff? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Staff.FirstOrDefault(x => x.StaffId == id);
    }

    /// <summary>The staff profile belonging to a user account, or null.</summary>
    public static Staff? GetByUserId(int userId)
    {
        using var context = new LanguageCenterContext();
        return context.Staff.FirstOrDefault(x => x.UserId == userId);
    }

    public static void Save(Staff entity)
    {
        using var context = new LanguageCenterContext();
        context.Staff.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Staff entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Staff.Find(entity.StaffId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Staff.Find(id);
        if (existing == null) return;
        context.Staff.Remove(existing);
        context.SaveChanges();
    }
}
