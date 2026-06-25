using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

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

    public static void Save(Admin entity)
    {
        using var context = new LanguageCenterContext();
        context.Admins.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Admin entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Admins.Find(entity.AdminId)
            ?? throw new Exception("Admin not found.");
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Admins.Find(id)
            ?? throw new Exception("Admin not found.");
        context.Admins.Remove(existing);
        context.SaveChanges();
    }
}
